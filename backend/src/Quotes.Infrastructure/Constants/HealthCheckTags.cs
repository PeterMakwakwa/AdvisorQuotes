namespace Quotes.Infrastructure.Constants
{
    /// <summary>
    /// Tags that decide which health endpoints run a check.
    /// </summary>
    public static class HealthCheckTags
    {
        /// <summary>
        /// Checks that must pass before an instance takes traffic (/health/ready).
        /// </summary>
        public const string Readiness = "ready";

        public const string Outbox = "outbox";
    }
}
