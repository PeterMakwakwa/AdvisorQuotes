namespace Quotes.Domain.Observability.Messages
{
    /// <summary>
    /// Information-level log templates (named placeholders) and Healthy health check descriptions (string.Format placeholders).
    /// </summary>
    public static class InfoMessages
    {
        public const string RequestRejected = "{Operation} rejected with {ErrorCode}: {ErrorMessage}";

        public const string EventPublished = "Published {EventType} {MessageId}: {Payload}";

        public const string OutboxDispatcherDisabled = "Outbox dispatcher is disabled";

        public const string MaintenanceCompleted =
            "Maintenance removed {IdempotencyKeys} expired idempotency keys and {OutboxMessages} published outbox messages in {Statements} delete statements";

        public const string MaintenanceDisabled = "Database maintenance is disabled";

        public const string OutboxPending = "{0} outbox message(s) pending.";
    }
}
