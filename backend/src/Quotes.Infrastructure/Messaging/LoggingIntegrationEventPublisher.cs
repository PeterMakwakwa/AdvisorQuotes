using Microsoft.Extensions.Logging;

namespace Quotes.Infrastructure.Messaging
{
    /// <summary>
    /// The local publisher: writes each event to the log as structured data. Runs with nothing but PostgreSQL.
    /// </summary>
    internal sealed class LoggingIntegrationEventPublisher : IIntegrationEventPublisher
    {
        private readonly ILogger<LoggingIntegrationEventPublisher> _logger;

        public LoggingIntegrationEventPublisher(ILogger<LoggingIntegrationEventPublisher> logger)
        {
            _logger = logger;
        }

        public Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken)
        {
            _logger.EventPublished(message.EventType, message.Id, message.Payload);

            return Task.CompletedTask;
        }
    }
}
