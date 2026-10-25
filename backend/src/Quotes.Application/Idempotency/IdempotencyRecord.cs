namespace Quotes.Application.Idempotency
{
    /// <summary>
    /// Remembers which quote a submission created, so a repeat of the same submission returns that quote instead of a new one.
    /// </summary>
    public sealed class IdempotencyRecord
    {
        private IdempotencyRecord()
        {
            // Used by EF Core when loading from the database
        }

        public string AdviserId { get; private init; } = string.Empty;

        public string Key { get; private init; } = string.Empty;

        public string RequestHash { get; private set; } = string.Empty;

        public Guid QuoteId { get; private set; }

        public DateTimeOffset CreatedAt { get; private set; }

        public DateTimeOffset ExpiresAt { get; private set; }

        public static IdempotencyRecord Create(
            string adviserId,
            string key,
            string requestHash,
            Guid quoteId,
            DateTimeOffset now,
            TimeSpan lifetime)
        {
            return new IdempotencyRecord
            {
                AdviserId = adviserId,
                Key = key,
                RequestHash = requestHash,
                QuoteId = quoteId,
                CreatedAt = now,
                ExpiresAt = now.Add(lifetime)
            };
        }

        public bool IsExpired(DateTimeOffset now)
        {
            return now >= ExpiresAt;
        }

        public bool Matches(string requestHash)
        {
            return string.Equals(RequestHash, requestHash, StringComparison.Ordinal);
        }

        /// <summary>
        /// Points an expired key at a new quote. The row is reused because (adviser, key) is the primary key.
        /// </summary>
        public void Renew(string requestHash, Guid quoteId, DateTimeOffset now, TimeSpan lifetime)
        {
            RequestHash = requestHash;
            QuoteId = quoteId;
            CreatedAt = now;
            ExpiresAt = now.Add(lifetime);
        }
    }
}
