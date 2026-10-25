using System.ComponentModel.DataAnnotations;

namespace Quotes.Infrastructure.Persistence
{
    /// <summary>
    /// Retry of transient database failures (dropped connections, failover, too many connections). Safe for creates
    /// because the idempotency key turns a retried insert into a replay, never a second quote.
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
