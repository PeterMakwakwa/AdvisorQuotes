using FluentValidation;
using FluentValidation.Results;
using Quotes.Application.Abstractions;
using Quotes.Application.Common;
using Quotes.Application.Errors;
using Quotes.Application.Exceptions;
using Quotes.Application.Interfaces;
using Quotes.Application.Models.Commands;
using Quotes.Application.Models.Request.V1;
using Quotes.Application.Models.Response.V1;
using Quotes.Domain.Common;
using Quotes.Domain.Entities;
using Quotes.Domain.Errors;
using Quotes.Domain.Repositories;

namespace Quotes.Application.Handlers
{
    /// <summary>
    /// Moves a quote to a new status through the aggregate, which enforces the transition rules, records the history and
    /// raises the event. Tracing, metrics and logging live in <c>InstrumentedChangeQuoteStatusHandler</c>.
    /// </summary>
    public sealed class ChangeQuoteStatusHandler : ICommandHandler<ChangeQuoteStatusCommand, QuoteResponse>
    {
        private readonly IValidator<ChangeQuoteStatusRequest> _validator;

        private readonly IQuoteRepository _quoteRepository;

        private readonly IUnitOfWork _unitOfWork;

        private readonly TimeProvider _timeProvider;

        public ChangeQuoteStatusHandler(
            IValidator<ChangeQuoteStatusRequest> validator,
            IQuoteRepository quoteRepository,
            IUnitOfWork unitOfWork,
            TimeProvider timeProvider)
        {
            _validator = validator;
            _quoteRepository = quoteRepository;
            _unitOfWork = unitOfWork;
            _timeProvider = timeProvider;
        }

        public async Task<Result<QuoteResponse>> HandleAsync(ChangeQuoteStatusCommand command, CancellationToken cancellationToken)
        {
            ValidationResult validation = await _validator.ValidateAsync(command.Request, cancellationToken);

            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            Quote? quote = await _quoteRepository.GetByIdAsync(command.QuoteId, cancellationToken);

            if (quote is null)
            {
                return QuoteErrors.NotFound(command.QuoteId);
            }

            Result changed = quote.ChangeStatus(command.Request.BuildCommand(), _timeProvider.GetUtcNow());

            if (changed.IsFailure)
            {
                return changed.Error!;
            }

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (ConcurrencyConflictException)
            {
                return ConcurrencyErrors.QuoteModified(command.QuoteId);
            }

            return QuoteResponse.FromDomain(quote);
        }
    }
}
