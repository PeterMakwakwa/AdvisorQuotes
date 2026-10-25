using Quotes.Domain.Common;
using Quotes.Domain.Enums;

namespace Quotes.Domain.Events
{
    /// <summary>
    /// A quote moved to a new status (for example SentToClient → Accepted).
    /// </summary>
    public sealed record QuoteStatusChanged : IDomainEvent
    {
        public required Guid EventId { get; init; }

        public required DateTimeOffset OccurredAt { get; init; }

        public required Guid QuoteId { get; init; }

        public required string ExternalReference { get; init; }

        public required QuoteStatus FromStatus { get; init; }

        public required QuoteStatus ToStatus { get; init; }

        public string? Reason { get; init; }

        public required string ChangedBy { get; init; }
    }
}
