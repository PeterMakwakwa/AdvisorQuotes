namespace Quotes.Domain.Observability.Messages
{
    /// <summary>
    /// Warning-level log templates (named placeholders) and Degraded health check descriptions (string.Format placeholders).
    /// </summary>
    public static class WarningMessages
    {
        public const string PublishFailed = "Publishing {EventType} {MessageId} failed (attempt {Attempt} of {MaxAttempts}); retrying in {RetryDelay}";

        public const string LeaseLost =
            "The lease on {EventType} {MessageId} expired before its outcome was recorded; another dispatcher has taken it over "
            + "and may publish it again. Consider a longer Outbox:LeaseDuration or a smaller Outbox:BatchSize";

        public const string RateLimitExceeded = "Rate limit exceeded for {Client} on {Method} {Path}";

        public const string OutboxDeadLettered = "{0} outbox message(s) gave up after {1} attempts.";

        public const string OutboxBacklogAged = "The oldest pending outbox message has waited {0:N0} s.";
    }
}
