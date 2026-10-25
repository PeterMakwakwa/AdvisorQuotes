using Microsoft.Extensions.Options;
using Quotes.Api.Networking;
using Quotes.Domain.Observability.Messages;

namespace Quotes.Api.RateLimiting
{
    /// <summary>
    /// Runs at start-up (ValidateOnStart): the limiter partitions by client address, which is only the real client when
    /// forwarded headers are honoured. Development is exempt, because there the browser talks to the API directly.
    /// </summary>
    internal sealed class RateLimitingOptionsValidator : IValidateOptions<RateLimitingOptions>
    {
        private readonly IOptions<ForwardedHeadersSettings> _forwardedHeaders;

        private readonly IHostEnvironment _environment;

        public RateLimitingOptionsValidator(IOptions<ForwardedHeadersSettings> forwardedHeaders, IHostEnvironment environment)
        {
            _forwardedHeaders = forwardedHeaders;
            _environment = environment;
        }

        public ValidateOptionsResult Validate(string? name, RateLimitingOptions options)
        {
            if (options.RequireForwardedHeaders && !_forwardedHeaders.Value.Enabled && !_environment.IsDevelopment())
            {
                return ValidateOptionsResult.Fail(ErrorMessages.ForwardedHeadersRequiredForRateLimiting);
            }

            return ValidateOptionsResult.Success;
        }
    }
}
