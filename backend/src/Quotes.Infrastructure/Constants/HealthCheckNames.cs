namespace Quotes.Infrastructure.Constants
{
    /// <summary>
    /// Names shown for each check in the /health response.
    /// </summary>
    public static class HealthCheckNames
    {
        public const string Database = "postgres";

        public const string Outbox = "outbox";
    }
}
