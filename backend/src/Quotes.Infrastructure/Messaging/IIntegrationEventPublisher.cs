namespace Quotes.Infrastructure.Messaging
{
    /// <summary>
    /// Where outbox messages are published. Locally they are logged (no broker is needed); in AWS this would be an
    /// SNS or EventBridge publisher, chosen in configuration without changing the dispatcher.
    /// </summary>
    public interface IIntegrationEventPublisher
    {
        Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken);
    }
}
