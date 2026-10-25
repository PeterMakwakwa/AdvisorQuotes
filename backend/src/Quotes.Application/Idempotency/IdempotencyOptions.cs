using System.ComponentModel.DataAnnotations;

namespace Quotes.Application.Idempotency
{
    public sealed class IdempotencyOptions
    {
        public const string SectionName = "Idempotency";

        /// <summary>
        /// How long a client-supplied Idempotency-Key is remembered.
        /// </summary>
        [Range(typeof(TimeSpan), "00:01:00", "7.00:00:00", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
        public TimeSpan KeyLifetime { get; init; } = TimeSpan.FromHours(24);

        /// <summary>
        /// When no key is sent, an identical body from the same adviser within this window is treated as a duplicate.
        /// </summary>
        [Range(typeof(TimeSpan), "00:00:10", "1.00:00:00", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
        public TimeSpan FingerprintWindow { get; init; } = TimeSpan.FromMinutes(10);
    }
}
