using Microsoft.Extensions.Logging;
using Quotes.Domain.Observability.Messages;

namespace Quotes.Infrastructure.Messaging
{
    /// <summary>
    /// Source-generated log messages for the outbox. Event ids 2000-2999.
    /// </summary>
    internal static partial class MessagingLog
    {
        [LoggerMessage(EventId = 2001, Level = LogLevel.Information, Message = InfoMessages.EventPublished)]
        public static partial void EventPublished(this ILogger logger, string eventType, Guid messageId, string payload);

        [LoggerMessage(EventId = 2002, Level = LogLevel.Warning, Message = WarningMessages.PublishFailed)]
        public static partial void PublishFailed(
            this ILogger logger, Exception exception, string eventType, Guid messageId, int attempt, int maxAttempts, TimeSpan retryDelay);

        [LoggerMessage(EventId = 2003, Level = LogLevel.Error, Message = ErrorMessages.PublishGaveUp)]
        public static partial void PublishGaveUp(this ILogger logger, Exception exception, string eventType, Guid messageId, int attempts);

        [LoggerMessage(EventId = 2004, Level = LogLevel.Warning, Message = WarningMessages.LeaseLost)]
        public static partial void LeaseLost(this ILogger logger, string eventType, Guid messageId);

        [LoggerMessage(EventId = 2005, Level = LogLevel.Information, Message = InfoMessages.OutboxDispatcherDisabled)]
        public static partial void DispatcherDisabled(this ILogger logger);

        [LoggerMessage(EventId = 2006, Level = LogLevel.Error, Message = ErrorMessages.OutboxDispatchFailed)]
        public static partial void DispatchFailed(this ILogger logger, Exception exception);
    }
}
