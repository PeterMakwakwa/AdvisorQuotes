using System.Diagnostics;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Quotes.Api.Constants;
using Quotes.Application.Observability;
using Quotes.Infrastructure.Messaging;

namespace Quotes.Api.Extensions
{
    /// <summary>
    /// The end-to-end signal: OpenTelemetry distributed tracing. One trace per request contains the HTTP span,
    /// the application spans (quote.create, pricing.calculate, quote.status.change) and every SQL statement.
    /// Metrics add the totals: quotes created, status changes, duplicates caught and rejections.
    /// </summary>
    public static class TelemetryExtensions
    {
        private const int DefaultMetricsExportIntervalSeconds = 15;

        private const int MillisecondsPerSecond = 1000;

        public static IServiceCollection AddQuotesTelemetry(this IServiceCollection services, IConfiguration configuration)
        {
            string serviceName = configuration[ConfigurationKeys.TelemetryServiceName] ?? TelemetryNames.DefaultServiceName;
            bool useConsoleExporter = configuration.GetValue(ConfigurationKeys.TelemetryConsoleExporter, defaultValue: true);
            bool useOtlpExporter = !string.IsNullOrWhiteSpace(configuration[ConfigurationKeys.OtlpEndpoint]);
            bool useConsoleMetricsExporter = configuration.GetValue(ConfigurationKeys.TelemetryConsoleMetricsExporter, defaultValue: false);
            bool useOtlpMetrics = useOtlpExporter && configuration.GetValue(ConfigurationKeys.TelemetryOtlpMetrics, defaultValue: false);
            int metricsExportIntervalMs = MillisecondsPerSecond
                * configuration.GetValue(ConfigurationKeys.TelemetryMetricsExportInterval, DefaultMetricsExportIntervalSeconds);

            services.AddOpenTelemetry()
                .ConfigureResource(resource => resource.AddService(serviceName))
                .WithTracing(tracing =>
                {
                    tracing
                        .AddSource(QuotesTelemetry.ActivitySourceName)
                        .AddSource(MessagingTelemetry.SourceName)
                        .AddAspNetCoreInstrumentation(options =>
                        {
                            options.Filter = context => !context.Request.Path.StartsWithSegments(EndpointPaths.Health)
                                && !context.Request.Path.StartsWithSegments(EndpointPaths.Metrics);
                            options.RecordException = true;
                        })
                        .AddNpgsql();

                    // OTLP (for example to Jaeger) when an endpoint is configured; the console exporter needs no
                    // external service, so the trace can be followed with nothing but PostgreSQL running.
                    if (useOtlpExporter)
                    {
                        tracing.AddOtlpExporter();
                    }

                    if (useConsoleExporter)
                    {
                        tracing.AddConsoleExporter();
                    }
                })
                .WithMetrics(metrics =>
                {
                    // Business metrics plus the built-in ASP.NET Core (request rate, duration) and .NET runtime meters.
                    metrics
                        .AddMeter(QuotesMetrics.MeterName)
                        .AddMeter(MessagingTelemetry.SourceName)
                        .AddAspNetCoreInstrumentation()
                        .AddMeter(TelemetryNames.RateLimitingMeter)
                        .AddMeter(TelemetryNames.RuntimeMeter);

                    // Metrics go over OTLP only when asked: Jaeger accepts traces but not metrics, whereas an
                    // OpenTelemetry collector (ADOT in AWS) accepts both.
                    if (useOtlpMetrics)
                    {
                        metrics.AddOtlpExporter((_, reader) =>
                            reader.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = metricsExportIntervalMs);
                    }

                    if (useConsoleMetricsExporter)
                    {
                        metrics.AddConsoleExporter((_, reader) =>
                            reader.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = metricsExportIntervalMs);
                    }
                });

            return services;
        }

        /// <summary>
        /// Returns the trace id on every response, so a caller or operator can look up the trace and the matching logs.
        /// </summary>
        public static IApplicationBuilder UseTraceIdResponseHeader(this IApplicationBuilder app)
        {
            return app.Use(async (context, next) =>
            {
                string? traceId = Activity.Current?.TraceId.ToString();

                if (traceId is not null)
                {
                    context.Response.OnStarting(() =>
                    {
                        context.Response.Headers[HttpHeaderNames.TraceId] = traceId;

                        return Task.CompletedTask;
                    });
                }

                await next(context);
            });
        }
    }
}
