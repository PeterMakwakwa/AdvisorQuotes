using Quotes.Domain.Common;

namespace Quotes.Domain.Events
{
    /// <summary>
    /// A quote was captured and priced.
    /// </summary>
    public sealed record QuoteCreated : IDomainEvent
    {
        public required Guid EventId { get; init; }

        public required DateTimeOffset OccurredAt { get; init; }

        public required Guid QuoteId { get; init; }

        public required string ExternalReference { get; init; }

        public required string AdviserId { get; init; }

        public string? AdviserReference { get; init; }

        public required string ProductCode { get; init; }

        public required decimal CoverAmount { get; init; }

        public required int TermYears { get; init; }

        public required decimal MonthlyPremium { get; init; }

        public required string Currency { get; init; }

        public required string PricingVersion { get; init; }
    }
}
