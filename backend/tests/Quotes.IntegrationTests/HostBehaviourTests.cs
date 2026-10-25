using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Quotes.Api.Logging;
using Quotes.Application.Interfaces;
using Quotes.Application.Models.Queries;
using Quotes.Application.Models.Response.V1;
using Quotes.IntegrationTests.Infrastructure;
using Shouldly;

namespace Quotes.IntegrationTests
{
    /// <summary>
    /// How the host behaves around the use cases: unexpected errors, bad configuration, proxies and log format.
    /// </summary>
    [Collection(ApiCollection.Name)]
    public class HostBehaviourTests
    {
        private const string SecretFailureDetail = "connection to 10.0.0.5 refused: password authentication failed";

        private readonly QuotesApiFactory _factory;

        public HostBehaviourTests(QuotesApiFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task UnhandledException_Returns500ProblemWithoutLeakingDetails()
        {
            await using WebApplicationFactory<Program> failing = _factory.WithWebHostBuilder(builder =>
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IQuoteReadService>();
                    services.AddScoped<IQuoteReadService, FailingQuoteReadService>();
                }));
            HttpClient client = failing.CreateClient();

            HttpResponseMessage response = await client.GetAsync($"{ApiRequests.QuotesPath}/{Guid.CreateVersion7()}");

            response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
            response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
            response.Headers.Contains("X-Trace-Id").ShouldBeTrue();
            string body = await response.Content.ReadAsStringAsync();
            body.ShouldNotContain(SecretFailureDetail);
            body.ShouldNotContain("FailingQuoteReadService");
            using JsonDocument problem = JsonDocument.Parse(body);
            problem.RootElement.GetProperty("status").GetInt32().ShouldBe(500);
            problem.RootElement.GetProperty("title").GetString().ShouldBe("An unexpected error occurred.");
            problem.RootElement.TryGetProperty("traceId", out _).ShouldBeTrue();
        }

        [Theory]
        [InlineData("Pricing:SmokerLoading", "0.5", "SmokerLoading")]
        [InlineData("Pricing:AgeBands:1:From", "25", "overlap")]
        [InlineData("Idempotency:KeyLifetime", "00:00:01", "KeyLifetime")]
        [InlineData("RateLimiting:PermitLimit", "0", "PermitLimit")]
        [InlineData("Outbox:BatchSize", "0", "BatchSize")]
        [InlineData("Maintenance:DeleteBatchSize", "0", "DeleteBatchSize")]
        [InlineData("ForwardedHeaders:KnownNetworks:0", "not-a-network", "KnownNetworks")]
        public void InvalidSetting_StopsStartup(string key, string value, string expectedInMessage)
        {
            using WebApplicationFactory<Program> misconfigured = _factory.WithWebHostBuilder(builder => builder.UseSetting(key, value));

            Exception exception = Should.Throw<Exception>(() => misconfigured.CreateClient());

            exception.ToString().ShouldContain(expectedInMessage);
        }

        [Fact]
        public async Task ForwardedHeaders_Disabled_ClientsBehindTheSameProxyShareOneLimit()
        {
            await using WebApplicationFactory<Program> limited = _factory.WithWebHostBuilder(builder =>
                builder.UseSetting("RateLimiting:PermitLimit", "1"));

            (await SendViaProxyAsync(limited, "203.0.113.10")).ShouldBe(StatusCodes.Status200OK);
            (await SendViaProxyAsync(limited, "203.0.113.20")).ShouldBe(StatusCodes.Status429TooManyRequests);
        }

        [Fact]
        public async Task ForwardedHeaders_Enabled_EachForwardedClientHasItsOwnLimit()
        {
            await using WebApplicationFactory<Program> limited = _factory.WithWebHostBuilder(builder =>
            {
                builder.UseSetting("RateLimiting:PermitLimit", "1");
                builder.UseSetting("ForwardedHeaders:Enabled", "true");
            });

            (await SendViaProxyAsync(limited, "203.0.113.10")).ShouldBe(StatusCodes.Status200OK);
            (await SendViaProxyAsync(limited, "203.0.113.20")).ShouldBe(StatusCodes.Status200OK);
            (await SendViaProxyAsync(limited, "203.0.113.10")).ShouldBe(StatusCodes.Status429TooManyRequests);
        }

        [Theory]
        [InlineData(null, "Development", false)]
        [InlineData(null, "Production", true)]
        [InlineData(null, "Testing", true)]
        [InlineData("text", "Production", false)]
        [InlineData("json", "Development", true)]
        public void LogFormat_IsTextInDevelopmentAndJsonElsewhere_UnlessConfigured(string? format, string environment, bool expectJson)
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { [LoggingExtensions.FormatKey] = format })
                .Build();
            IHostEnvironment hostEnvironment = new HostingEnvironment { EnvironmentName = environment };

            LoggingExtensions.UsesJson(configuration, hostEnvironment).ShouldBe(expectJson);
        }

        /// <summary>
        /// A request arriving from the load balancer (loopback, a trusted proxy by default) on behalf of a client.
        /// </summary>
        private static async Task<int> SendViaProxyAsync(WebApplicationFactory<Program> factory, string clientAddress)
        {
            HttpContext context = await factory.Server.SendAsync(request =>
            {
                request.Request.Method = HttpMethods.Get;
                request.Request.Path = "/" + ApiRequests.QuotesPath;
                request.Request.QueryString = new QueryString("?pageSize=1");
                request.Request.Headers["X-Forwarded-For"] = clientAddress;
                request.Connection.RemoteIpAddress = IPAddress.Loopback;
            });

            return context.Response.StatusCode;
        }

        private sealed class FailingQuoteReadService : IQuoteReadService
        {
            public Task<QuoteResponse?> GetByIdAsync(Guid quoteId, CancellationToken cancellationToken)
            {
                throw new InvalidOperationException(SecretFailureDetail);
            }

            public Task<PagedResponse<QuoteSummaryResponse>> ListAsync(ListQuotesQuery query, CancellationToken cancellationToken)
            {
                throw new InvalidOperationException(SecretFailureDetail);
            }
        }
    }
}
