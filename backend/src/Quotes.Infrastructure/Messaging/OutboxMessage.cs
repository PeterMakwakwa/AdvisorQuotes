namespace Quotes.Infrastructure.Messaging
{
    /// <summary>
    /// A domain event waiting to be published (src/dbQuotes/up/0007 and 0008). Its id is the event id, so consumers can
    /// ignore a message they have already processed (delivery is at least once).
    /// </summary>
    public sealed class OutboxMessage
    {
        private const int MaxErrorLength = 2000;

        private OutboxMessage()
        {
            // Used by EF Core when loading from the database
        }

        public Guid Id { get; private init; }

        /// <summary>
        /// The aggregate (quote) that raised the event. Messages for one aggregate are published in the order they occurred.
        /// </summary>
        public Guid AggregateId { get; private init; }

        public string EventType { get; private init; } = string.Empty;

        public string Payload { get; private init; } = string.Empty;

        public DateTimeOffset OccurredAt { get; private init; }

        /// <summary>
        /// W3C trace context of the request that raised the event, so publishing continues the same trace.
        /// </summary>
        public string? TraceParent { get; private init; }

        public int Attempts { get; private set; }

        /// <summary>
        /// After a failure, the earliest time the message is tried again (exponential backoff). Null when it can go now.
        /// </summary>
        public DateTimeOffset? NextAttemptAt { get; private set; }

        public DateTimeOffset? ProcessedAt { get; private set; }

        public string? LastError { get; private set; }

        /// <summary>
        /// While in the future, a dispatcher has claimed the message and is publishing it.
        /// </summary>
        public DateTimeOffset? LockedUntil { get; private set; }

        /// <summary>
        /// Changes on every claim. Saving an outcome checks it, so only the current lease holder can record a result.
        /// </summary>
        public Guid? LockToken { get; private set; }

        internal static OutboxMessage Create(
            Guid id,
            Guid aggregateId,
            string eventType,
            string payload,
            DateTimeOffset occurredAt,
            string? traceParent)
        {
            return new OutboxMessage
            {
                Id = id,
                AggregateId = aggregateId,
                EventType = eventType,
                Payload = payload,
                OccurredAt = occurredAt,
                TraceParent = traceParent
            };
        }

        internal void MarkPublished(DateTimeOffset publishedAt)
        {
            Attempts++;
            ProcessedAt = publishedAt;
            NextAttemptAt = null;
            LastError = null;
            ReleaseLease();
        }

        internal void RecordFailure(string error, DateTimeOffset nextAttemptAt)
        {
            Attempts++;
            NextAttemptAt = nextAttemptAt;
            LastError = error.Length <= MaxErrorLength ? error : error[..MaxErrorLength];
            ReleaseLease();
        }

        private void ReleaseLease()
        {
            LockedUntil = null;
            LockToken = null;
        }
    }
}
