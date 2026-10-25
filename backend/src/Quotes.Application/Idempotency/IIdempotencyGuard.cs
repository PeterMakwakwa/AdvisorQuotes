namespace Quotes.Application.Idempotency
{
    /// <summary>
    /// Protects a create command against duplicate submissions, with or without an Idempotency-Key.
    /// Usage: <see cref="CheckAsync"/> → (create the resource) → <see cref="Remember"/> → save in one unit of work;
    /// if the save loses a race to a concurrent identical submission, <see cref="RecheckAfterConflictAsync"/>.
    /// </summary>
    public interface IIdempotencyGuard
    {
        /// <summary>
        /// Looks for an earlier submission with the same key, or with the same body when no key was sent.
        /// </summary>
        /// <param name="scope">Whose keys these are (the adviser).</param>
        /// <param name="clientKey">The Idempotency-Key header, if any.</param>
        /// <param name="canonicalRequest">The request in its normalised form; its hash identifies "the same body".</param>
        Task<IdempotencyCheck> CheckAsync<TRequest>(string scope, string? clientKey, TRequest canonicalRequest, CancellationToken cancellationToken);

        /// <summary>
        /// Stages "this key created this resource" in the current unit of work, so it commits with the resource or not at all.
        /// </summary>
        void Remember(IdempotencyCheck check, Guid resourceId);

        /// <summary>
        /// After the save failed because a concurrent submission saved the same key first: the decision against the
        /// winner's record (replay it, or reject a different body). Null if no winner can be found.
        /// </summary>
        Task<IdempotencyCheck?> RecheckAfterConflictAsync(IdempotencyCheck check, CancellationToken cancellationToken);
    }
}
