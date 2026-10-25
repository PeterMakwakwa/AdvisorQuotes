using Prometheus;

namespace Quotes.Api.Extensions
{
    /// <summary>
    /// A Prometheus scrape endpoint for every System.Diagnostics.Metrics meter: the business metrics (quotes.*),
    /// ASP.NET Core and the .NET runtime. Useful locally (curl /metrics) and for any Prometheus-compatible scraper.
    /// </summary>
    public static class PrometheusExtensions
    {
        public const string MetricsPath = "/metrics";

        /// <summary>
        /// prometheus-net's built-in meter listener only starts on the first scrape, so anything recorded before it would be
        /// lost. It is replaced by one started with the application (once per process).
        /// </summary>
        private static readonly Lazy<IDisposable> MeterListener = new Lazy<IDisposable>(() =>
        {
            Metrics.SuppressDefaultMetrics(new SuppressDefaultMetricOptions { SuppressMeters = true });

            return MeterAdapter.StartListening();
        });

        public static IEndpointRouteBuilder MapPrometheusMetrics(this IEndpointRouteBuilder endpoints)
        {
            _ = MeterListener.Value;

            endpoints.MapMetrics(MetricsPath);

            return endpoints;
        }
    }
}
