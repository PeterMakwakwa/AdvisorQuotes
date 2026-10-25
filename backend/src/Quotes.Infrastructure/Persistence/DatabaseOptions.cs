using System.ComponentModel.DataAnnotations;

namespace Quotes.Infrastructure.Persistence
{
    /// <summary>
    /// Retry of transient database failures (dropped connections, failover, too many connections). A save is retried
    /// as a whole transaction; if the commit itself fails, the unit of work first checks whether it committed, so a
    /// retry never writes the same change twice. A client that retries after an error is protected by its idempotency key.
    /// </summary>
    public sealed class DatabaseOptions
    {
        public const string SectionName = "Database";

        [Range(0, 10)]
        public int MaxRetryCount { get; init; } = 3;

        [Range(typeof(TimeSpan), "00:00:00", "00:01:00", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
        public TimeSpan MaxRetryDelay { get; init; } = TimeSpan.FromSeconds(5);
    }
}
