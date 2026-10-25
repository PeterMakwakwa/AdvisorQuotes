using Quotes.Domain.Common;

namespace Quotes.Application.Idempotency
{
    /// <summary>
    /// The guard's decision about a submission: go ahead, replay an earlier result, or reject.
    /// </summary>
    public sealed record IdempotencyCheck
    {
        /// <summary>
        /// Whose keys these are (the adviser), so two advisers can use the same key independently.
        /// </summary>
        public required string Scope { get; init; }

        public required string Key { get; init; }

        public required string RequestHash { get; init; }

        public required IdempotencyKeySource KeySource { get; init; }

        public required TimeSpan Lifetime { get; init; }

        /// <summary>
        /// The id of the resource created by the earlier, identical submission. When set, return it instead of creating one.
        /// </summary>
        public Guid? ReplayOf { get; init; }

        /// <summary>
        /// Why the submission must be refused (key too long, or the key was used with a different body).
        /// </summary>
        public Error? Rejection { get; init; }

        public bool CanProceed => ReplayOf is null && Rejection is null;

        /// <summary>
        /// An expired record for the same key, renewed in place when the new resource is remembered.
        /// </summary>
        internal IdempotencyRecord? ExpiredRecord { get; init; }
    }
}
