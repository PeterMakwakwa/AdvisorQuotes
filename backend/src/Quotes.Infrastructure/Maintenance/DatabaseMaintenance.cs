using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quotes.Infrastructure.Persistence;

namespace Quotes.Infrastructure.Maintenance
{
    /// <summary>
    /// Keeps the support tables small: deletes idempotency keys well past expiry and published outbox messages past
    /// retention. Deletes run in batches, each its own short statement, so a large backlog (for example after a long
    /// outage) never becomes one huge transaction. Quotes, their history and dead-lettered messages are never touched.
    /// </summary>
    public sealed class DatabaseMaintenance
    {
        private readonly QuotesDbContext _dbContext;

        private readonly MaintenanceOptions _options;

        private readonly TimeProvider _timeProvider;

        private readonly ILogger<DatabaseMaintenance> _logger;

        public DatabaseMaintenance(
            QuotesDbContext dbContext,
            IOptions<MaintenanceOptions> options,
            TimeProvider timeProvider,
            ILogger<DatabaseMaintenance> logger)
        {
            _dbContext = dbContext;
            _options = options.Value;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        public async Task<MaintenanceResult> RunAsync(CancellationToken cancellationToken)
        {
            DateTimeOffset now = _timeProvider.GetUtcNow();
            DateTimeOffset keyCutoff = now - _options.ExpiredIdempotencyKeyRetention;
            DateTimeOffset outboxCutoff = now - _options.ProcessedOutboxRetention;

            (int keysDeleted, int keyStatements) = await DeleteInBatchesAsync(() => _dbContext.IdempotencyRecords
                .Where(record => record.ExpiresAt < keyCutoff)
                .Take(_options.DeleteBatchSize)
                .ExecuteDeleteAsync(cancellationToken));

            (int messagesDeleted, int messageStatements) = await DeleteInBatchesAsync(() => _dbContext.OutboxMessages
                .Where(message => message.ProcessedAt != null && message.ProcessedAt < outboxCutoff)
                .Take(_options.DeleteBatchSize)
                .ExecuteDeleteAsync(cancellationToken));

            _logger.MaintenanceCompleted(keysDeleted, messagesDeleted, keyStatements + messageStatements);

            return new MaintenanceResult
            {
                IdempotencyKeysDeleted = keysDeleted,
                OutboxMessagesDeleted = messagesDeleted,
                DeleteStatements = keyStatements + messageStatements
            };
        }

        private async Task<(int Deleted, int Statements)> DeleteInBatchesAsync(Func<Task<int>> deleteBatch)
        {
            int total = 0;
            int statements = 0;
            int deleted;

            do
            {
                deleted = await deleteBatch();
                total += deleted;
                statements++;
            }
            while (deleted == _options.DeleteBatchSize);

            return (total, statements);
        }
    }

    public sealed record MaintenanceResult
    {
        public required int IdempotencyKeysDeleted { get; init; }

        public required int OutboxMessagesDeleted { get; init; }

        /// <summary>
        /// How many DELETE statements ran (each at most DeleteBatchSize rows).
        /// </summary>
        public required int DeleteStatements { get; init; }
    }
}
