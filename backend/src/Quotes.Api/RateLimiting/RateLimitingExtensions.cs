using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Quotes.Api.Constants;
using Quotes.Api.Logging;
using Quotes.Domain.Errors;
using Quotes.Domain.Observability.Messages;

namespace Quotes.Api.RateLimiting
{
    /// <summary>
    /// A fixed window per client on the quote endpoints, so one misbehaving client cannot starve the others.
    /// Without authentication the client is identified by IP address; behind a load balancer, enable forwarded
    /// headers so this is the real client IP. With authentication it would be the adviser from the token.
    /// </summary>
    public static class RateLimitingExtensions
    {
        public static IServiceCollection AddQuotesRateLimiting(this IServiceCollection services)
        {
            services.AddOptions<RateLimitingOptions>()
                .BindConfiguration(RateLimitingOptions.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddRateLimiter(limiter =>
            {
                limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                limiter.AddPolicy(RateLimitingPolicies.Quotes, context =>
                {
                    RateLimitingOptions options = context.RequestServices.GetRequiredService<IOptions<RateLimitingOptions>>().Value;

                    return RateLimitPartition.GetFixedWindowLimiter(
                        context.Connection.RemoteIpAddress?.ToString() ?? RateLimitingPolicies.UnknownClient,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = options.PermitLimit,
                            Window = options.Window,
                            QueueLimit = 0
                        });
                });

                limiter.OnRejected = OnRejectedAsync;
            });

            return services;
        }

        private static async ValueTask OnRejectedAsync(OnRejectedContext context, CancellationToken cancellationToken)
        {
            HttpContext httpContext = context.HttpContext;
            RateLimitingOptions options = httpContext.RequestServices.GetRequiredService<IOptions<RateLimitingOptions>>().Value;
            TimeSpan retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan leaseRetryAfter)
                ? leaseRetryAfter
                : options.Window;

            httpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);

            httpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                .CreateLogger(typeof(RateLimitingExtensions))
                .RateLimitExceeded(httpContext.Connection.RemoteIpAddress?.ToString(), httpContext.Request.Method, httpContext.Request.Path.ToString());

            ProblemDetails problem = new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = ResponseMessages.TooManyRequestsTitle,
                Detail = MessageFormatter.Format(ResponseMessages.TooManyRequests, Math.Ceiling(retryAfter.TotalSeconds))
            };
            problem.Extensions[ProblemDetailsKeys.ErrorCode] = ErrorCodes.RateLimitExceeded;

            await httpContext.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = problem
            });
        }
    }
}
