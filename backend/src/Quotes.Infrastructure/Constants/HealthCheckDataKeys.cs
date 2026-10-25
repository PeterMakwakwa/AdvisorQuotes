namespace Quotes.Infrastructure.Constants
{
    /// <summary>
    /// Keys of the data a health check adds to its entry in the /health response.
    /// </summary>
    public static class HealthCheckDataKeys
    {
        public const string Pending = "pending";

        public const string DeadLettered = "deadLettered";

        public const string OldestPendingAgeSeconds = "oldestPendingAgeSeconds";
    }
}
