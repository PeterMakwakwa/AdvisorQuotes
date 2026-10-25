using Microsoft.Extensions.Logging;
using Quotes.Domain.Enums;

namespace Quotes.Application.Observability
{
    /// <summary>
    /// Source-generated log messages (no boxing or parsing at runtime, and nothing evaluated when the level is off).
    /// Event ids 1000-1999 belong to the Application layer.
    /// </summary>
    internal static partial class ApplicationLog
    {
        [LoggerMessage(EventId = 1001, Level = LogLevel.Information,
            Message = "Quote {QuoteId} ({ExternalReference}) created for adviser {AdviserId}: {Premium} {Currency} per month, pricing {PricingVersion}")]
        public static partial void QuoteCreated(
            this ILogger logger, Guid quoteId, string externalReference, string adviserId, decimal premium, string currency, string pricingVersion);

        [LoggerMessage(EventId = 1002, Level = LogLevel.Information,
            Message = "Duplicate submission for adviser {AdviserId} replayed quote {QuoteId} (detected by {KeySource})")]
        public static partial void QuoteReplayed(this ILogger logger, string adviserId, Guid quoteId, string keySource);

        [LoggerMessage(EventId = 1003, Level = LogLevel.Information,
            Message = "Quote {QuoteId} moved from {FromStatus} to {ToStatus} by {ChangedBy}")]
        public static partial void QuoteStatusChanged(this ILogger logger, Guid quoteId, QuoteStatus? fromStatus, QuoteStatus toStatus, string changedBy);

        [LoggerMessage(EventId = 1004, Level = LogLevel.Information,
            Message = "{Operation} rejected with {ErrorCode}: {ErrorMessage}")]
        public static partial void RequestRejected(this ILogger logger, string operation, string errorCode, string errorMessage);
    }
}
