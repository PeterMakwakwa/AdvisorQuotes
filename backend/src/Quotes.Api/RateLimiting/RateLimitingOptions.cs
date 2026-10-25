using System.ComponentModel.DataAnnotations;

namespace Quotes.Api.RateLimiting
{
    public sealed class RateLimitingOptions
    {
        public const string SectionName = "RateLimiting";

        /// <summary>
        /// Requests allowed per client in each window.
        /// </summary>
        [Range(1, 100_000)]
        public int PermitLimit { get; init; } = 100;

        [Range(typeof(TimeSpan), "00:00:01", "01:00:00", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
        public TimeSpan Window { get; init; } = TimeSpan.FromMinutes(1);
    }
}
