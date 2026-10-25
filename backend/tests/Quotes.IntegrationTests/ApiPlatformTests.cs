using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Quotes.IntegrationTests.Infrastructure;
using Shouldly;

namespace Quotes.IntegrationTests
{
    /// <summary>
    /// The cross-cutting HTTP behaviour: API versioning, the OpenAPI document, rate limiting and the Prometheus endpoint.
    /// </summary>
    [Collection(ApiCollection.Name)]
    public class ApiPlatformTests
    {
        private readonly QuotesApiFactory _factory;

        private readonly HttpClient _client;

        public ApiPlatformTests(QuotesApiFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Responses_ReportTheSupportedApiVersions()
        {
            HttpResponseMessage response = await _client.GetAsync($"{ApiRequests.QuotesPath}?pageSize=1");

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            response.Headers.GetValues("api-supported-versions").ShouldContain("1.0");
        }

        [Fact]
        public async Task UnsupportedApiVersion_IsNotFound()
        {
            HttpResponseMessage response = await _client.GetAsync("api/v2/quotes");

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task OpenApiDocument_DescribesTheVersionedApi()
        {
            HttpResponseMessage response = await _client.GetAsync("openapi/v1.json");

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            document.RootElement.GetProperty("info").GetProperty("title").GetString().ShouldBe("Advisor Quotes API");
            JsonElement paths = document.RootElement.GetProperty("paths");
            paths.TryGetProperty("/api/v1/quotes", out _).ShouldBeTrue();
            paths.TryGetProperty("/api/v1/quotes/{quoteId}/status-changes", out _).ShouldBeTrue();
            (await response.Content.ReadAsStringAsync()).ShouldContain("Idempotency-Key");
        }

        [Fact]
        public async Task RateLimit_Exceeded_Returns429ProblemWithRetryAfter()
        {
            await using WebApplicationFactory<Program> limited = _factory.WithWebHostBuilder(builder =>
                builder.UseSetting("RateLimiting:PermitLimit", "2"));
            HttpClient client = limited.CreateClient();

            HttpResponseMessage first = await client.GetAsync($"{ApiRequests.QuotesPath}?pageSize=1");
            HttpResponseMessage second = await client.GetAsync($"{ApiRequests.QuotesPath}?pageSize=1");
            HttpResponseMessage third = await client.GetAsync($"{ApiRequests.QuotesPath}?pageSize=1");
            HttpResponseMessage health = await client.GetAsync("health/live");

            first.StatusCode.ShouldBe(HttpStatusCode.OK);
            second.StatusCode.ShouldBe(HttpStatusCode.OK);
            third.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
            third.Headers.RetryAfter.ShouldNotBeNull();
            using JsonDocument problem = JsonDocument.Parse(await third.Content.ReadAsStringAsync());
            problem.RootElement.GetProperty("code").GetString().ShouldBe("RateLimit.Exceeded");
            health.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task PrometheusEndpoint_ExposesTheBusinessMetrics()
        {
            HttpResponseMessage created = await ApiRequests.PostQuoteAsync(
                _client, ApiRequests.Quote(ApiRequests.NewAdviserId()), Guid.NewGuid().ToString());
            created.StatusCode.ShouldBe(HttpStatusCode.Created);

            HttpResponseMessage response = await _client.GetAsync("metrics");

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            string metrics = await response.Content.ReadAsStringAsync();
            metrics.ShouldContain("quotes_created");
            metrics.ShouldContain("quotes_monthly_premium");
        }
    }
}
