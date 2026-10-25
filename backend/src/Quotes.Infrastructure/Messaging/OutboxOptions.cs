using System.ComponentModel.DataAnnotations;

namespace Quotes.Infrastructure.Messaging
{
    public sealed class OutboxOptions
    {
        public const string SectionName = "Outbox";

        /// <summary>
        /// Runs the background dispatcher. Disabled in the integration tests, which call the dispatcher directly.
        /// </summary>
        public bool Enabled { get; init; } = true;

        [Range(typeof(TimeSpan), "00:00:00.100", "00:05:00", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
        public TimeSpan PollingInterval { get; init; } = TimeSpan.FromSeconds(2);

        [Range(1, 1000)]
        public int BatchSize { get; init; } = 50;

        /// <summary>
        /// After this many failed attempts a message is no longer picked up; it stays in the table for investigation.
        /// With the default backoff, 20 attempts span roughly an hour and a half.
        /// </summary>
        [Range(1, 100)]
        public int MaxAttempts { get; init; } = 20;

        /// <summary>
        /// How long a claimed batch is reserved for one dispatcher. Must comfortably exceed the time to publish a whole
        /// batch; if the dispatcher dies, another one picks the messages up after this.
        /// </summary>
        [Range(typeof(TimeSpan), "00:00:05", "01:00:00", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
        public TimeSpan LeaseDuration { get; init; } = TimeSpan.FromMinutes(1);

        [Range(typeof(TimeSpan), "00:00:00", "01:00:00", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
        public TimeSpan InitialRetryDelay { get; init; } = TimeSpan.FromSeconds(5);

        [Range(typeof(TimeSpan), "00:00:00", "1.00:00:00", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
        public TimeSpan MaxRetryDelay { get; init; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// The outbox health check reports Degraded when the oldest pending message has waited longer than this.
        /// </summary>
        [Range(typeof(TimeSpan), "00:00:10", "1.00:00:00", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
        public TimeSpan BacklogAgeWarning { get; init; } = TimeSpan.FromMinutes(5);
    }
}
