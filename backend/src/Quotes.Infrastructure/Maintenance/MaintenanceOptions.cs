using System.ComponentModel.DataAnnotations;

namespace Quotes.Infrastructure.Maintenance
{
    public sealed class MaintenanceOptions
    {
        public const string SectionName = "Maintenance";

        public bool Enabled { get; init; } = true;

        [Range(typeof(TimeSpan), "00:00:01", "1.00:00:00", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
        public TimeSpan Interval { get; init; } = TimeSpan.FromHours(1);

        /// <summary>
        /// How long after expiry an idempotency key is deleted. The margin keeps a key that is being renewed by a
        /// late retry from disappearing underneath it.
        /// </summary>
        [Range(typeof(TimeSpan), "00:00:00", "30.00:00:00", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
        public TimeSpan ExpiredIdempotencyKeyRetention { get; init; } = TimeSpan.FromDays(1);

        /// <summary>
        /// How long published outbox messages are kept for troubleshooting.
        /// </summary>
        [Range(typeof(TimeSpan), "00:00:00", "365.00:00:00", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
        public TimeSpan ProcessedOutboxRetention { get; init; } = TimeSpan.FromDays(7);

        /// <summary>
        /// Rows deleted per statement. Keeps each delete short, however much has built up.
        /// </summary>
        [Range(1, 100_000)]
        public int DeleteBatchSize { get; init; } = 5_000;
    }
}
