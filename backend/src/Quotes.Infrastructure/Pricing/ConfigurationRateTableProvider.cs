using Microsoft.Extensions.Options;
using Quotes.Application.Interfaces;
using Quotes.Domain.Pricing;

namespace Quotes.Infrastructure.Pricing
{
    /// <summary>
    /// Serves the rate table from configuration. IOptionsMonitor picks up changes without a restart.
    /// A database- or service-backed provider can replace this without touching the use cases.
    /// </summary>
    internal sealed class ConfigurationRateTableProvider : IRateTableProvider
    {
        private readonly IOptionsMonitor<PricingOptions> _options;

        public ConfigurationRateTableProvider(IOptionsMonitor<PricingOptions> options)
        {
            _options = options;
        }

        public Task<RateTable> GetCurrentAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_options.CurrentValue.ToRateTable());
        }
    }
}
