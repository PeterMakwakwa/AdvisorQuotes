namespace Quotes.Api.Constants
{
    /// <summary>
    /// Single configuration values read directly by the host. Whole sections bind to options classes through their SectionName.
    /// </summary>
    public static class ConfigurationKeys
    {
        public const string OpenApiEnabled = "OpenApi:Enabled";

        public const string LoggingFormat = "Logging:Format";

        public const string TelemetryServiceName = "Telemetry:ServiceName";

        public const string TelemetryConsoleExporter = "Telemetry:ConsoleExporter";

        public const string TelemetryConsoleMetricsExporter = "Telemetry:ConsoleMetricsExporter";

        public const string TelemetryOtlpMetrics = "Telemetry:OtlpMetrics";

        public const string TelemetryMetricsExportInterval = "Telemetry:MetricsExportIntervalSeconds";

        /// <summary>
        /// The standard OpenTelemetry environment variable; when set, traces are exported over OTLP.
        /// </summary>
        public const string OtlpEndpoint = "OTEL_EXPORTER_OTLP_ENDPOINT";
    }
}
