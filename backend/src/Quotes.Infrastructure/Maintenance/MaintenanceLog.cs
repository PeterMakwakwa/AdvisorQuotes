using Microsoft.Extensions.Logging;

namespace Quotes.Infrastructure.Maintenance
{
    /// <summary>
    /// Source-generated log messages for database maintenance. Event ids 3000-3999.
    /// </summary>
    internal static partial class MaintenanceLog
    {
        [LoggerMessage(EventId = 3001, Level = LogLevel.Information,
            Message = "Maintenance removed {IdempotencyKeys} expired idempotency keys and {OutboxMessages} published outbox messages in {Statements} delete statements")]
        public static partial void MaintenanceCompleted(this ILogger logger, int idempotencyKeys, int outboxMessages, int statements);

        [LoggerMessage(EventId = 3002, Level = LogLevel.Information, Message = "Database maintenance is disabled")]
        public static partial void MaintenanceDisabled(this ILogger logger);

        [LoggerMessage(EventId = 3003, Level = LogLevel.Error, Message = "Database maintenance failed; retrying on the next interval")]
        public static partial void MaintenanceFailed(this ILogger logger, Exception exception);
    }
}
