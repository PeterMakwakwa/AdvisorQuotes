namespace Quotes.Api.Constants
{
    /// <summary>
    /// Names registered with OpenTelemetry by the host. The application's own sources and meters are named in
    /// QuotesTelemetry, QuotesMetrics and MessagingTelemetry.
    /// </summary>
    public static class TelemetryNames
    {
        public const string DefaultServiceName = "advisor-quotes-api";

        public const string RuntimeMeter = "System.Runtime";

        public const string RateLimitingMeter = "Microsoft.AspNetCore.RateLimiting";
    }
}
