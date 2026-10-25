using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quotes.Infrastructure.Persistence;

namespace Quotes.Infrastructure.Messaging
{
    /// <summary>
    /// Publishes one batch of pending outbox messages using claim-with-lease:
    /// <list type="number">
    /// <item><b>Claim</b> the batch in one short statement (FOR UPDATE SKIP LOCKED + a lease), committed immediately.</item>
    /// <item><b>Publish</b> outside any transaction: no database lock is held, however slow the publisher is.</item>
    /// <item><b>Record</b> each outcome on its own, only while this dispatcher still holds the lease (lock_token check).</item>
    /// </list>
    /// Several instances can dispatch at once; a dispatcher that dies leaves its messages to be reclaimed when the lease
    /// expires. Failed messages back off exponentially. Delivery is at least once.
    /// </summary>
    public sealed class OutboxDispatcher
    {
        private const string OutboxSystem = "outbox";

        private const string PublishOperation = "publish";

        private readonly QuotesDbContext _dbContext;

        private readonly IIntegrationEventPublisher _publisher;

        private readonly OutboxOptions _options;

        private readonly MessagingTelemetry _telemetry;

        private readonly TimeProvider _timeProvider;

        private readonly ILogger<OutboxDispatcher> _logger;

        public OutboxDispatcher(
            QuotesDbContext dbContext,
            IIntegrationEventPublisher publisher,
            IOptions<OutboxOptions> options,
            MessagingTelemetry telemetry,
            TimeProvider timeProvider,
            ILogger<OutboxDispatcher> logger)
        {
            _dbContext = dbContext;
            _publisher = publisher;
            _options = options.Value;
            _telemetry = telemetry;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        public async Task<OutboxDispatchResult> DispatchPendingAsync(CancellationToken cancellationToken)
        {
            _dbContext.ChangeTracker.Clear();

            List<OutboxMessage> claimed = await ClaimBatchAsync(cancellationToken);
            int published = 0;

            foreach (OutboxMessage message in claimed)
            {
                bool succeeded = await PublishAsync(message, cancellationToken);

                if (await TryRecordOutcomeAsync(message) && succeeded)
                {
                    published++;
                }
            }

            return new OutboxDispatchResult { PickedUp = claimed.Count, Published = published };
        }

        /// <summary>
        /// One atomic statement: picks the oldest messages that are due and not leased by anyone else, skipping rows another
        /// dispatcher is claiming right now, and stamps them with this dispatcher's lease. It commits on its own, so the
        /// row locks last only for this statement.
        /// </summary>
        private async Task<List<OutboxMessage>> ClaimBatchAsync(CancellationToken cancellationToken)
        {
            DateTimeOffset now = _timeProvider.GetUtcNow();
            DateTimeOffset lockedUntil = now + _options.LeaseDuration;
            Guid lockToken = Guid.NewGuid();

            List<OutboxMessage> claimed = await _dbContext.OutboxMessages
                .FromSql($"""
                    UPDATE outbox_messages
                    SET locked_until = {lockedUntil}, lock_token = {lockToken}
                    WHERE id IN (
                        SELECT id FROM outbox_messages
                        WHERE processed_at IS NULL
                          AND attempts < {_options.MaxAttempts}
                          AND (next_attempt_at IS NULL OR next_attempt_at <= {now})
                          AND (locked_until IS NULL OR locked_until <= {now})
                        ORDER BY occurred_at, id
                        LIMIT {_options.BatchSize}
                        FOR UPDATE SKIP LOCKED)
                    RETURNING *
                    """)
                .ToListAsync(cancellationToken);

            // RETURNING does not preserve the subquery's order.
            return claimed.OrderBy(message => message.OccurredAt).ThenBy(message => message.Id).ToList();
        }

        private async Task<bool> PublishAsync(OutboxMessage message, CancellationToken cancellationToken)
        {
            // Continue the trace of the request that raised the event, so the publish shows up in that request's trace.
            ActivityContext.TryParse(message.TraceParent, null, out ActivityContext parent);

            using Activity? activity = MessagingTelemetry.ActivitySource.StartActivity(
                $"{message.EventType} {PublishOperation}",
                ActivityKind.Producer,
                parent);
            activity?.SetTag(MessagingTelemetry.TagNames.System, OutboxSystem);
            activity?.SetTag(MessagingTelemetry.TagNames.Operation, PublishOperation);
            activity?.SetTag(MessagingTelemetry.TagNames.EventType, message.EventType);
            activity?.SetTag(MessagingTelemetry.TagNames.MessageId, message.Id);
            activity?.SetTag(MessagingTelemetry.TagNames.Attempt, message.Attempts + 1);

            try
            {
                await _publisher.PublishAsync(message, cancellationToken);

                DateTimeOffset publishedAt = _timeProvider.GetUtcNow();
                message.MarkPublished(publishedAt);
                _telemetry.Published(message.EventType, publishedAt - message.OccurredAt);

                return true;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                TimeSpan delay = OutboxRetryPolicy.DelayAfter(message.Attempts + 1, _options.InitialRetryDelay, _options.MaxRetryDelay);
                message.RecordFailure(exception.Message, _timeProvider.GetUtcNow() + delay);
                _telemetry.Failed(message.EventType);
                activity?.SetStatus(ActivityStatusCode.Error, exception.Message);

                if (message.Attempts >= _options.MaxAttempts)
                {
                    _logger.PublishGaveUp(exception, message.EventType, message.Id, message.Attempts);
                }
                else
                {
                    _logger.PublishFailed(exception, message.EventType, message.Id, message.Attempts, _options.MaxAttempts, delay);
                }

                return false;
            }
        }

        /// <summary>
        /// Saves one message's outcome on its own, so a problem with one message never loses the others' results.
        /// The update carries "AND lock_token = ours"; if the lease expired and another dispatcher took over, it fails
        /// with a concurrency conflict and the new holder's result stands. CancellationToken.None: the message may already
        /// have been published, so the outcome is recorded even while the host is stopping.
        /// </summary>
        private async Task<bool> TryRecordOutcomeAsync(OutboxMessage message)
        {
            try
            {
                await _dbContext.SaveChangesAsync(CancellationToken.None);

                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                _dbContext.Entry(message).State = EntityState.Detached;

                _logger.LeaseLost(message.EventType, message.Id);

                return false;
            }
        }
    }

    public sealed record OutboxDispatchResult
    {
        public required int PickedUp { get; init; }

        public required int Published { get; init; }
    }
}
