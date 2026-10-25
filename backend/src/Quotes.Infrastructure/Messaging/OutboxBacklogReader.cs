using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Quotes.Infrastructure.Persistence;

namespace Quotes.Infrastructure.Messaging
{
    /// <summary>
    /// How far behind the outbox is: messages still to publish, messages that gave up after the maximum attempts,
    /// and how long the oldest pending message has been waiting. One query on the pending-messages index.
    /// </summary>
    public sealed class OutboxBacklogReader
    {
        private readonly QuotesDbContext _dbContext;

        private readonly OutboxOptions _options;

        private readonly TimeProvider _timeProvider;

        public OutboxBacklogReader(QuotesDbContext dbContext, IOptions<OutboxOptions> options, TimeProvider timeProvider)
        {
            _dbContext = dbContext;
            _options = options.Value;
            _timeProvider = timeProvider;
        }

        public async Task<OutboxBacklog> ReadAsync(CancellationToken cancellationToken)
        {
            BacklogRow row = await _dbContext.Database
                .SqlQuery<BacklogRow>($"""
                    SELECT count(*) FILTER (WHERE attempts < {_options.MaxAttempts})::int AS "Pending",
                           count(*) FILTER (WHERE attempts >= {_options.MaxAttempts})::int AS "DeadLettered",
                           min(occurred_at) FILTER (WHERE attempts < {_options.MaxAttempts}) AS "OldestPendingAt"
                    FROM outbox_messages
                    WHERE processed_at IS NULL
                    """)
                .SingleAsync(cancellationToken);

            TimeSpan oldestPendingAge = row.OldestPendingAt is null
                ? TimeSpan.Zero
                : _timeProvider.GetUtcNow() - row.OldestPendingAt.Value;

            return new OutboxBacklog
            {
                Pending = row.Pending,
                DeadLettered = row.DeadLettered,
                OldestPendingAge = oldestPendingAge < TimeSpan.Zero ? TimeSpan.Zero : oldestPendingAge
            };
        }

        private sealed class BacklogRow
        {
            public int Pending { get; set; }

            public int DeadLettered { get; set; }

            public DateTimeOffset? OldestPendingAt { get; set; }
        }
    }

    public sealed record OutboxBacklog
    {
        public static readonly OutboxBacklog Empty = new OutboxBacklog { Pending = 0, DeadLettered = 0, OldestPendingAge = TimeSpan.Zero };

        public required int Pending { get; init; }

        /// <summary>
        /// Messages that used up their attempts. They are never deleted automatically and need investigation.
        /// </summary>
        public required int DeadLettered { get; init; }

        public required TimeSpan OldestPendingAge { get; init; }
    }
}
