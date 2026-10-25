using Quotes.Domain.Observability.Messages;

namespace Quotes.Api.Logging
{
    /// <summary>
    /// Source-generated log messages for the HTTP host. Event ids 4000-4999.
    /// </summary>
    internal static partial class ApiLog
    {
        [LoggerMessage(EventId = 4001, Level = LogLevel.Error, Message = ErrorMessages.UnhandledException)]
        public static partial void UnhandledException(this ILogger logger, Exception exception, string method, string path);

        [LoggerMessage(EventId = 4002, Level = LogLevel.Warning, Message = WarningMessages.RateLimitExceeded)]
        public static partial void RateLimitExceeded(this ILogger logger, string? client, string method, string path);
    }
}
