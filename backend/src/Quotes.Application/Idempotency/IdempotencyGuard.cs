using Microsoft.Extensions.Options;
using Quotes.Application.Constants;
using Quotes.Application.Errors;
using Quotes.Application.Interfaces;

namespace Quotes.Application.Idempotency
{
    /// <summary>
    /// With a client key, the key is honoured for the full key lifetime. Without one, an identical body from the same
    /// scope within a short window is treated as an accidental double submit. The same key with a different body is
    /// rejected. The (scope, key) primary key decides races between concurrent submissions.
    /// </summary>
    public sealed class IdempotencyGuard : IIdempotencyGuard
    {
        private const string FingerprintKeyPrefix = "fp:";

        private readonly IIdempotencyStore _store;

        private readonly IdempotencyOptions _options;

        private readonly TimeProvider _timeProvider;

        public IdempotencyGuard(IIdempotencyStore store, IOptions<IdempotencyOptions> options, TimeProvider timeProvider)
        {
            _store = store;
            _options = options.Value;
            _timeProvider = timeProvider;
        }

        public async Task<IdempotencyCheck> CheckAsync<TRequest>(
            string scope,
            string? clientKey,
            TRequest canonicalRequest,
            CancellationToken cancellationToken)
        {
            string? key = string.IsNullOrWhiteSpace(clientKey) ? null : clientKey.Trim();
            string requestHash = RequestHasher.Compute(canonicalRequest);

            IdempotencyCheck pending = key is null
                ? new IdempotencyCheck
                {
                    Scope = scope,
                    Key = FingerprintKeyPrefix + requestHash,
                    RequestHash = requestHash,
                    KeySource = IdempotencyKeySource.Fingerprint,
                    Lifetime = _options.FingerprintWindow
                }
                : new IdempotencyCheck
                {
                    Scope = scope,
                    Key = key,
                    RequestHash = requestHash,
                    KeySource = IdempotencyKeySource.Client,
                    Lifetime = _options.KeyLifetime
                };

            if (pending.Key.Length > FieldLengths.IdempotencyKey)
            {
                return pending with { Rejection = IdempotencyErrors.KeyTooLong };
            }

            IdempotencyRecord? record = await _store.FindAsync(pending.Scope, pending.Key, cancellationToken);

            return Evaluate(pending, record);
        }

        public void Remember(IdempotencyCheck check, Guid resourceId)
        {
            DateTimeOffset now = _timeProvider.GetUtcNow();

            if (check.ExpiredRecord is null)
            {
                _store.Add(IdempotencyRecord.Create(check.Scope, check.Key, check.RequestHash, resourceId, now, check.Lifetime));
            }
            else
            {
                check.ExpiredRecord.Renew(check.RequestHash, resourceId, now, check.Lifetime);
            }
        }

        public async Task<IdempotencyCheck?> RecheckAfterConflictAsync(IdempotencyCheck check, CancellationToken cancellationToken)
        {
            IdempotencyRecord? winner = await _store.FindAsync(check.Scope, check.Key, cancellationToken);
            IdempotencyCheck decision = Evaluate(check with { ExpiredRecord = null }, winner);

            return decision.CanProceed ? null : decision;
        }

        private IdempotencyCheck Evaluate(IdempotencyCheck pending, IdempotencyRecord? record)
        {
            if (record is null)
            {
                return pending;
            }

            if (record.IsExpired(_timeProvider.GetUtcNow()))
            {
                return pending with { ExpiredRecord = record };
            }

            return record.Matches(pending.RequestHash)
                ? pending with { ReplayOf = record.QuoteId }
                : pending with { Rejection = IdempotencyErrors.KeyReusedWithDifferentRequest };
        }
    }
}
