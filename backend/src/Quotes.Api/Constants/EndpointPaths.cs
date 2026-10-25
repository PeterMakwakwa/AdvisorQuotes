namespace Quotes.Api.Constants
{
    /// <summary>
    /// Operational endpoints. The quote endpoints keep their routes on the controller.
    /// </summary>
    public static class EndpointPaths
    {
        /// <summary>
        /// Every check, including the outbox backlog, for operators and dashboards.
        /// </summary>
        public const string Health = "/health";

        /// <summary>
        /// The process is running (no dependencies checked).
        /// </summary>
        public const string HealthLive = "/health/live";

        /// <summary>
        /// The database is reachable and migrated, so the instance can take traffic.
        /// </summary>
        public const string HealthReady = "/health/ready";

        public const string Metrics = "/metrics";
    }
}
