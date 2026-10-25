using Quotes.Domain.Commands;
using Quotes.Domain.Common;
using Quotes.Domain.Enums;
using Quotes.Domain.Errors;
using Quotes.Domain.Events;
using Quotes.Domain.Rules;
using Quotes.Domain.ValueObjects;

namespace Quotes.Domain.Entities
{
    public sealed class Quote : AggregateRoot
    {
        private const string CreatedReason = "Quote created";

        private readonly List<StatusChange> _statusHistory = [];

        private Quote()
        {
            // Used by EF Core when loading from the database
        }

        public Guid Id { get; private init; }

        public string ExternalReference { get; private init; } = string.Empty;

        public string AdviserId { get; private init; } = string.Empty;

        /// <summary>
        /// The adviser's own reference (for example a CRM case number). ExternalReference is the one this service issues.
        /// </summary>
        public string? AdviserReference { get; private init; }

        public ClientDetails Client { get; private init; } = null!;

        public ProductDetails Product { get; private init; } = null!;

        public Money MonthlyPremium { get; private init; } = null!;

        public string PricingVersion { get; private init; } = string.Empty;

        public QuoteStatus Status { get; private set; }

        public string? Notes { get; private init; }

        public DateTimeOffset CreatedAt { get; private init; }

        public DateTimeOffset UpdatedAt { get; private set; }

        public IReadOnlyCollection<StatusChange> StatusHistory => _statusHistory.AsReadOnly();

        public static Result<Quote> Create(CreateQuote command, DateTimeOffset now)
        {
            if (string.IsNullOrWhiteSpace(command.AdviserId))
            {
                return QuoteErrors.AdviserRequired;
            }

            if (string.IsNullOrWhiteSpace(command.Client.FullName))
            {
                return QuoteErrors.ClientNameRequired;
            }

            if (command.Client.DateOfBirth >= DateOnly.FromDateTime(now.UtcDateTime))
            {
                return QuoteErrors.DateOfBirthNotInPast;
            }

            if (command.Product.CoverAmount <= 0)
            {
                return QuoteErrors.CoverAmountNotPositive;
            }

            if (command.Product.TermYears <= 0)
            {
                return QuoteErrors.TermNotPositive;
            }

            if (command.MonthlyPremium.Amount < 0)
            {
                return QuoteErrors.PremiumNegative;
            }

            Quote quote = new Quote
            {
                Id = Guid.CreateVersion7(now),
                ExternalReference = command.ExternalReference,
                AdviserId = command.AdviserId.Trim(),
                AdviserReference = string.IsNullOrWhiteSpace(command.AdviserReference) ? null : command.AdviserReference.Trim(),
                Client = command.Client with { FullName = command.Client.FullName.Trim() },
                Product = command.Product,
                MonthlyPremium = command.MonthlyPremium,
                PricingVersion = command.PricingVersion,
                Status = QuoteStatus.Draft,
                Notes = string.IsNullOrWhiteSpace(command.Notes) ? null : command.Notes.Trim(),
                CreatedAt = now,
                UpdatedAt = now
            };

            quote._statusHistory.Add(
                new StatusChange(quote.Id, null, QuoteStatus.Draft, CreatedReason, quote.AdviserId, now));

            quote.Raise(new QuoteCreated
            {
                EventId = Guid.CreateVersion7(now),
                OccurredAt = now,
                QuoteId = quote.Id,
                ExternalReference = quote.ExternalReference,
                AdviserId = quote.AdviserId,
                AdviserReference = quote.AdviserReference,
                ProductCode = quote.Product.ProductCode,
                CoverAmount = quote.Product.CoverAmount,
                TermYears = quote.Product.TermYears,
                MonthlyPremium = quote.MonthlyPremium.Amount,
                Currency = quote.MonthlyPremium.Currency,
                PricingVersion = quote.PricingVersion
            });

            return quote;
        }

        public Result ChangeStatus(ChangeQuoteStatus command, DateTimeOffset now)
        {
            if (string.IsNullOrWhiteSpace(command.ChangedBy))
            {
                return Result.Failure(QuoteErrors.ChangedByRequired);
            }

            if (command.NewStatus == Status)
            {
                return Result.Failure(QuoteErrors.StatusUnchanged(Status));
            }

            if (!QuoteStatusTransitions.CanTransition(Status, command.NewStatus))
            {
                return Result.Failure(QuoteErrors.InvalidStatusTransition(Status, command.NewStatus));
            }

            if (command.NewStatus == QuoteStatus.Declined && string.IsNullOrWhiteSpace(command.Reason))
            {
                return Result.Failure(QuoteErrors.DeclineReasonRequired);
            }

            StatusChange change = new StatusChange(
                Id, Status, command.NewStatus, command.Reason?.Trim(), command.ChangedBy.Trim(), now);

            _statusHistory.Add(change);
            Status = command.NewStatus;
            UpdatedAt = now;

            Raise(new QuoteStatusChanged
            {
                EventId = Guid.CreateVersion7(now),
                OccurredAt = now,
                QuoteId = Id,
                ExternalReference = ExternalReference,
                FromStatus = change.FromStatus!.Value,
                ToStatus = change.ToStatus,
                Reason = change.Reason,
                ChangedBy = change.ChangedBy
            });

            return Result.Success();
        }
    }
}
