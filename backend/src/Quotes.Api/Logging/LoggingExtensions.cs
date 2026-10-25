using System.Globalization;
using Quotes.Api.Constants;
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
                    logger.WriteTo.Console(outputTemplate: LoggingFormats.TextTemplate, formatProvider: CultureInfo.InvariantCulture);
                }
            });

            return services;
        }

        public static bool UsesJson(IConfiguration configuration, IHostEnvironment environment)
        {
            string? format = configuration[ConfigurationKeys.LoggingFormat];

            if (string.Equals(format, LoggingFormats.Json, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(format, LoggingFormats.Text, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return !environment.IsDevelopment();
        }
    }
}
