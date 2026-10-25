using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Quotes.Application.Observability;

namespace Quotes.UnitTests.Fakes
{
    /// <summary>
    /// Real QuotesMetrics on a private meter factory. Collectors only see this instance's measurements,
    /// so tests running in parallel never count each other's quotes.
    /// </summary>
    internal sealed class TestMetrics : IDisposable
    {
        private readonly ServiceProvider _services;

        private readonly IMeterFactory _meterFactory;

        public TestMetrics()
        {
            _services = new ServiceCollection().AddMetrics().BuildServiceProvider();
            _meterFactory = _services.GetRequiredService<IMeterFactory>();
            Metrics = new QuotesMetrics(_meterFactory);
        }

        public QuotesMetrics Metrics { get; }

        public MetricCollector<T> Collect<T>(string instrumentName)
            where T : struct
        {
            return new MetricCollector<T>(_meterFactory, QuotesMetrics.MeterName, instrumentName);
        }

        public void Dispose()
        {
            _services.Dispose();
        }
    }
}
