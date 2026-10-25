using Quotes.Domain.Enums;

namespace Quotes.Domain.Entities
{
    public sealed class StatusChange
    {
        private StatusChange()
        {
            // Used by EF Core when loading from the database
        }

        internal StatusChange(
            Guid quoteId,
            QuoteStatus? fromStatus,
            QuoteStatus toStatus,
            string? reason,
            string changedBy,
            DateTimeOffset changedAt)
        {
            Id = Guid.CreateVersion7(changedAt);
            QuoteId = quoteId;
            FromStatus = fromStatus;
            ToStatus = toStatus;
            Reason = reason;
            ChangedBy = changedBy;
            ChangedAt = changedAt;
        }

        public Guid Id { get; private init; }

        public Guid QuoteId { get; private init; }

        public QuoteStatus? FromStatus { get; private init; }

        public QuoteStatus ToStatus { get; private init; }

        public string? Reason { get; private init; }

        public string ChangedBy { get; private init; } = string.Empty;

        public DateTimeOffset ChangedAt { get; private init; }
    }
}
