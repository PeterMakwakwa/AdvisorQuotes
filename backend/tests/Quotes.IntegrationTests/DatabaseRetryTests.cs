using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Quotes.Application.Models.Response.V1;
using Quotes.Infrastructure.Persistence;
using Quotes.IntegrationTests.Infrastructure;
using Shouldly;

namespace Quotes.IntegrationTests
{
    /// <summary>
    /// The execution strategy retries transient failures. When the failure hits the commit itself, the unit of work
    /// checks whether the transaction committed before retrying, so the retry never writes the change a second time.
    /// </summary>
    [Collection(ApiCollection.Name)]
    public class DatabaseRetryTests
    {
        private readonly QuotesApiFactory _factory;

        public DatabaseRetryTests(QuotesApiFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task ConnectionDroppedAfterCommit_CreateStillSucceeds_AndSavesTheQuoteOnce()
        {
            CommitThenDropConnectionInterceptor interceptor = new CommitThenDropConnectionInterceptor();
            await using WebApplicationFactory<Program> flaky = _factory.WithWebHostBuilder(builder =>
                builder.ConfigureTestServices(services =>
                    services.ConfigureDbContext<QuotesDbContext>(options => options.AddInterceptors(interceptor))));
            HttpClient client = flaky.CreateClient();
            string adviserId = ApiRequests.NewAdviserId();

            HttpResponseMessage response = await ApiRequests.PostQuoteAsync(client, ApiRequests.Quote(adviserId), Guid.NewGuid().ToString());

            interceptor.Triggered.ShouldBeTrue();
            response.StatusCode.ShouldBe(HttpStatusCode.Created);
            response.Headers.Contains("Idempotent-Replayed").ShouldBeFalse();
            QuoteResponse quote = (await response.Content.ReadFromJsonAsync<QuoteResponse>(ApiRequests.Json))!;
            (await _factory.CountRowsAsync("quotes", adviserId)).ShouldBe(1);
            (await _factory.CountRowsAsync("idempotency_keys", adviserId)).ShouldBe(1);
            (await _factory.GetOutboxRowsAsync(quote.Id)).ShouldHaveSingleItem();
        }
    }
}
