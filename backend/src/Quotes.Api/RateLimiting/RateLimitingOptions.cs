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

        /// <summary>
        /// Outside Development, start-up fails while ForwardedHeaders is disabled: behind a load balancer or reverse proxy
        /// every request would come from the proxy's address and all clients would share one limit. Set to false only when
        /// clients really do connect directly.
        /// </summary>
        public bool RequireForwardedHeaders { get; init; } = true;
    }
}
