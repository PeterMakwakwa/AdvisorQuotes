using FluentValidation;
using FluentValidation.Results;
using Quotes.Application.Abstractions;
using Quotes.Application.Common;
using Quotes.Application.Exceptions;
using Quotes.Application.Idempotency;
using Quotes.Application.Interfaces;
using Quotes.Application.Models.Commands;
using Quotes.Application.Models.Request.V1;
using Quotes.Application.Models.Response.V1;
using Quotes.Application.Models.Results;
using Quotes.Domain.Common;
using Quotes.Domain.Entities;
using Quotes.Domain.Errors;
using Quotes.Domain.Pricing;
using Quotes.Domain.Repositories;

namespace Quotes.Application.Handlers
{
    /// <summary>
    /// Captures a quote: validate → check for a duplicate submission → price → create → save the quote, its idempotency
    /// record and its events in one transaction. Duplicate detection lives in <see cref="IIdempotencyGuard"/>; tracing,
    /// metrics and logging in <c>InstrumentedCreateQuoteHandler</c>, which wraps this handler.
    /// </summary>
    public sealed class CreateQuoteHandler : ICommandHandler<CreateQuoteCommand, CreateQuoteResult>
    {
        private readonly IValidator<CreateQuoteRequest> _validator;

        private readonly IIdempotencyGuard _idempotencyGuard;

        private readonly IPricingService _pricingService;

        private readonly IQuoteReferenceGenerator _referenceGenerator;

        private readonly IQuoteRepository _quoteRepository;

        private readonly IUnitOfWork _unitOfWork;

        private readonly TimeProvider _timeProvider;

        public CreateQuoteHandler(
            IValidator<CreateQuoteRequest> validator,
            IIdempotencyGuard idempotencyGuard,
            IPricingService pricingService,
            IQuoteReferenceGenerator referenceGenerator,
            IQuoteRepository quoteRepository,
            IUnitOfWork unitOfWork,
            TimeProvider timeProvider)
        {
            _validator = validator;
            _idempotencyGuard = idempotencyGuard;
            _pricingService = pricingService;
            _referenceGenerator = referenceGenerator;
            _quoteRepository = quoteRepository;
            _unitOfWork = unitOfWork;
            _timeProvider = timeProvider;
        }

        public async Task<Result<CreateQuoteResult>> HandleAsync(CreateQuoteCommand command, CancellationToken cancellationToken)
        {
            CreateQuoteRequest request = command.Request;
            ValidationResult validation = await _validator.ValidateAsync(request, cancellationToken);

            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            IdempotencyCheck check = await _idempotencyGuard.CheckAsync(
                request.AdviserId.Trim(), command.IdempotencyKey, request.Normalize(), cancellationToken);

            if (!check.CanProceed)
            {
                return await ResolveAsync(check, cancellationToken);
            }

            Result<PremiumCalculation> pricing = await _pricingService.PriceAsync(
                request.Client.BuildClientDetails(),
                request.Product.BuildProductDetails(),
                cancellationToken);

            if (pricing.IsFailure)
            {
                return pricing.Error!;
            }

            string externalReference = await _referenceGenerator.NextAsync(cancellationToken);
            Result<Quote> created = Quote.Create(request.BuildCommand(externalReference, pricing.Value), _timeProvider.GetUtcNow());

            if (created.IsFailure)
            {
                return created.Error!;
            }

            Quote quote = created.Value;
            _quoteRepository.Add(quote);
            _idempotencyGuard.Remember(check, quote.Id);

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception) when (exception is DuplicateIdempotencyKeyException or ConcurrencyConflictException)
            {
                // A concurrent identical submission committed first; nothing of ours was saved.
                IdempotencyCheck? winner = await _idempotencyGuard.RecheckAfterConflictAsync(check, cancellationToken);

                if (winner is null)
                {
                    throw;
                }

                return await ResolveAsync(winner, cancellationToken);
            }

            return new CreateQuoteResult { Quote = QuoteResponse.FromDomain(quote), IsReplay = false, KeySource = check.KeySource };
        }

        /// <summary>
        /// A submission that must not create a quote: reject it, or return the quote the earlier submission created.
        /// </summary>
        private async Task<Result<CreateQuoteResult>> ResolveAsync(IdempotencyCheck check, CancellationToken cancellationToken)
        {
            if (check.Rejection is not null)
            {
                return check.Rejection;
            }

            Guid originalId = check.ReplayOf!.Value;
            Quote? original = await _quoteRepository.GetByIdAsync(originalId, cancellationToken);

            if (original is null)
            {
                return QuoteErrors.NotFound(originalId);
            }

            return new CreateQuoteResult { Quote = QuoteResponse.FromDomain(original), IsReplay = true, KeySource = check.KeySource };
        }
    }
}
