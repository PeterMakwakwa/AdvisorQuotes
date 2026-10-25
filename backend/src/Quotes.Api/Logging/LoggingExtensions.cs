using System.Globalization;
using Serilog;
using Serilog.Formatting.Compact;

namespace Quotes.Api.Logging
{
    /// <summary>
    /// Serilog to stdout. Readable text in Development; compact JSON everywhere else, so CloudWatch and other log stores can
    /// index the fields (including @tr, the trace id). Override with Logging:Format = "text" or "json".
    /// Levels and overrides still come from the "Serilog" configuration section.
    /// </summary>
    public static class LoggingExtensions
    {
        public const string FormatKey = "Logging:Format";

        public const string TextFormat = "text";

        public const string JsonFormat = "json";

        private const string TextTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] trace={TraceId} {SourceContext}: {Message:lj}{NewLine}{Exception}";

        public static IServiceCollection AddQuotesLogging(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
        {
            bool useJson = UsesJson(configuration, environment);

            services.AddSerilog((serviceProvider, logger) =>
            {
                logger
                    .ReadFrom.Configuration(configuration)
                    .ReadFrom.Services(serviceProvider)
                    .Enrich.FromLogContext();

                if (useJson)
                {
                    logger.WriteTo.Console(new RenderedCompactJsonFormatter());
                }
                else
                {
                    logger.WriteTo.Console(outputTemplate: TextTemplate, formatProvider: CultureInfo.InvariantCulture);
                }
            });

            return services;
        }

        public static bool UsesJson(IConfiguration configuration, IHostEnvironment environment)
        {
            string? format = configuration[FormatKey];

            if (string.Equals(format, JsonFormat, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(format, TextFormat, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return !environment.IsDevelopment();
        }
    }
}
