using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using Quotes.Domain.Observability.Messages;

namespace Quotes.Api.Networking
{
    public static class ForwardedHeadersExtensions
    {
        private const int MinForwardLimit = 1;

        private const int MaxForwardLimit = 10;

        /// <summary>
        /// Binds and validates <see cref="ForwardedHeadersSettings"/> (invalid addresses stop start-up) and applies them to
        /// ASP.NET Core's forwarded-headers middleware. Disabled, the middleware changes nothing.
        /// </summary>
        public static IServiceCollection AddQuotesForwardedHeaders(this IServiceCollection services)
        {
            services.AddOptions<ForwardedHeadersSettings>()
                .BindConfiguration(ForwardedHeadersSettings.SectionName)
                .Validate(settings => settings.KnownProxies.All(proxy => IPAddress.TryParse(proxy, out _)), ErrorMessages.KnownProxiesInvalid)
                .Validate(settings => settings.KnownNetworks.All(network => System.Net.IPNetwork.TryParse(network, out _)), ErrorMessages.KnownNetworksInvalid)
                .Validate(
                    settings => settings.ForwardLimit is >= MinForwardLimit and <= MaxForwardLimit,
                    MessageFormatter.Format(ErrorMessages.ForwardLimitOutOfRange, MinForwardLimit, MaxForwardLimit))
                .ValidateOnStart();

            services.AddOptions<ForwardedHeadersOptions>()
                .Configure<IOptions<ForwardedHeadersSettings>>((options, settings) => Apply(options, settings.Value));

            return services;
        }

        private static void Apply(ForwardedHeadersOptions options, ForwardedHeadersSettings settings)
        {
            if (!settings.Enabled)
            {
                options.ForwardedHeaders = ForwardedHeaders.None;

                return;
            }

            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = settings.ForwardLimit;

            foreach (string proxy in settings.KnownProxies)
            {
                options.KnownProxies.Add(IPAddress.Parse(proxy));
            }

            foreach (string network in settings.KnownNetworks)
            {
                options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
            }
        }
    }
}
