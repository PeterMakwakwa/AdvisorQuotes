using Microsoft.Extensions.Logging;

namespace Quotes.Infrastructure.Messaging
{
    /// <summary>
    /// Source-generated log messages for the outbox. Event ids 2000-2999.
    /// </summary>
    internal static partial class MessagingLog
    {
        [LoggerMessage(EventId = 2001, Level = LogLevel.Information, Message = "Published {EventType} {MessageId}: {Payload}")]
        public static partial void EventPublished(this ILogger logger, string eventType, Guid messageId, string payload);

        [LoggerMessage(EventId = 2002, Level = LogLevel.Warning,
            Message = "Publishing {EventType} {MessageId} failed (attempt {Attempt} of {MaxAttempts}); retrying in {RetryDelay}")]
        public static partial void PublishFailed(
            this ILogger logger, Exception exception, string eventType, Guid messageId, int attempt, int maxAttempts, TimeSpan retryDelay);

        [LoggerMessage(EventId = 2003, Level = LogLevel.Error,
            Message = "Giving up on {EventType} {MessageId} after {Attempts} attempts; it stays in the outbox for investigation")]
        public static partial void PublishGaveUp(this ILogger logger, Exception exception, string eventType, Guid messageId, int attempts);

        [LoggerMessage(EventId = 2004, Level = LogLevel.Warning,
            Message = "The lease on {EventType} {MessageId} expired before its outcome was recorded; another dispatcher has taken it over "
                + "and may publish it again. Consider a longer Outbox:LeaseDuration or a smaller Outbox:BatchSize")]
        public static partial void LeaseLost(this ILogger logger, string eventType, Guid messageId);

        [LoggerMessage(EventId = 2005, Level = LogLevel.Information, Message = "Outbox dispatcher is disabled")]
        public static partial void DispatcherDisabled(this ILogger logger);

        [LoggerMessage(EventId = 2006, Level = LogLevel.Error, Message = "Outbox dispatch failed; retrying on the next tick")]
        public static partial void DispatchFailed(this ILogger logger, Exception exception);
    }
}
