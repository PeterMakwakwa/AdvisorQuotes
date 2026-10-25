using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Quotes.Application.Interfaces;
using Quotes.Application.Models.Request.V1;
using Quotes.Application.Models.Response.V1;
using Quotes.Domain.Entities;
using Quotes.Domain.Enums;
using Quotes.Domain.Repositories;
using Quotes.IntegrationTests.Infrastructure;
using Shouldly;

namespace Quotes.IntegrationTests
{
    [Collection(ApiCollection.Name)]
    public class QuotesApiTests
    {
        private readonly QuotesApiFactory _factory;

        private readonly HttpClient _client;

        public QuotesApiTests(QuotesApiFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task CreateQuote_ReturnsCreatedPricedDraftWithLocationAndTraceId()
        {
            string adviserId = ApiRequests.NewAdviserId();

            HttpResponseMessage response = await ApiRequests.PostQuoteAsync(_client, ApiRequests.Quote(adviserId), "create-1");

            response.StatusCode.ShouldBe(HttpStatusCode.Created);
            response.Headers.Location.ShouldNotBeNull();
            response.Headers.Contains("X-Trace-Id").ShouldBeTrue();

            QuoteResponse quote = await ReadAsync<QuoteResponse>(response);
            quote.Status.ShouldBe(QuoteStatus.Draft);
            quote.Premium.MonthlyAmount.ShouldBe(300.00m);
            quote.Premium.Currency.ShouldBe("ZAR");
            quote.ExternalReference.ShouldStartWith($"Q-{DateTime.UtcNow.Year}-");
            quote.StatusHistory.ShouldHaveSingleItem().ToStatus.ShouldBe(QuoteStatus.Draft);
        }

        [Fact]
        public async Task CreateResponse_Timestamps_EqualTheStoredValues()
        {
            QuoteResponse created = await CreateQuoteAsync();

            QuoteResponse? fetched = await _client.GetFromJsonAsync<QuoteResponse>($"{ApiRequests.QuotesPath}/{created.Id}", ApiRequests.Json);

            // PostgreSQL keeps microseconds; the clock is truncated to match, so nothing changes on the way back.
            fetched!.CreatedAt.ShouldBe(created.CreatedAt);
            fetched.UpdatedAt.ShouldBe(created.UpdatedAt);
            fetched.StatusHistory.Single().ChangedAt.ShouldBe(created.StatusHistory.Single().ChangedAt);
            (created.CreatedAt.Ticks % TimeSpan.TicksPerMicrosecond).ShouldBe(0);
        }

        [Fact]
        public async Task CreateQuote_SameKeyTwice_ReturnsOriginalAndStoresOneQuote()
        {
            string adviserId = ApiRequests.NewAdviserId();

            QuoteResponse first = await ReadAsync<QuoteResponse>(
                await ApiRequests.PostQuoteAsync(_client, ApiRequests.Quote(adviserId), "retry-1"));
            HttpResponseMessage second = await ApiRequests.PostQuoteAsync(_client, ApiRequests.Quote(adviserId), "retry-1");

            second.StatusCode.ShouldBe(HttpStatusCode.Created);
            second.Headers.GetValues("Idempotent-Replayed").ShouldBe(["true"]);
            (await ReadAsync<QuoteResponse>(second)).Id.ShouldBe(first.Id);
            (await _factory.CountRowsAsync("quotes", adviserId)).ShouldBe(1);
        }

        [Fact]
        public async Task CreateQuote_SameKeyDifferentBody_Returns422()
        {
            string adviserId = ApiRequests.NewAdviserId();
            await ApiRequests.PostQuoteAsync(_client, ApiRequests.Quote(adviserId), "reuse-1");

            HttpResponseMessage response = await ApiRequests.PostQuoteAsync(_client, ApiRequests.Quote(adviserId, 2_000_000m), "reuse-1");

            response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
            (await ReadProblemCodeAsync(response)).ShouldBe("Idempotency.KeyReused");
        }

        [Fact]
        public async Task CreateQuote_WithoutKey_DoubleSubmitIsDeduplicated()
        {
            string adviserId = ApiRequests.NewAdviserId();

            HttpResponseMessage first = await ApiRequests.PostQuoteAsync(_client, ApiRequests.Quote(adviserId), idempotencyKey: null);
            HttpResponseMessage second = await ApiRequests.PostQuoteAsync(_client, ApiRequests.Quote(adviserId), idempotencyKey: null);

            first.StatusCode.ShouldBe(HttpStatusCode.Created);
            second.Headers.GetValues("Idempotent-Replayed").ShouldBe(["true"]);
            (await _factory.CountRowsAsync("quotes", adviserId)).ShouldBe(1);
        }

        [Fact]
        public async Task CreateQuote_ConcurrentIdenticalSubmissions_CreateExactlyOneQuote()
        {
            string adviserId = ApiRequests.NewAdviserId();

            HttpResponseMessage[] responses = await Task.WhenAll(
                Enumerable.Range(0, 8).Select(_ => ApiRequests.PostQuoteAsync(_client, ApiRequests.Quote(adviserId), "burst-1")));

            responses.ShouldAllBe(response => response.StatusCode == HttpStatusCode.Created);
            QuoteResponse[] quotes = await Task.WhenAll(responses.Select(ReadAsync<QuoteResponse>));
            quotes.Select(quote => quote.Id).Distinct().ShouldHaveSingleItem();
            (await _factory.CountRowsAsync("quotes", adviserId)).ShouldBe(1);
            (await _factory.CountRowsAsync("idempotency_keys", adviserId)).ShouldBe(1);
        }

        [Fact]
        public async Task CreateQuote_InvalidRequest_Returns400WithFieldErrors()
        {
            string adviserId = ApiRequests.NewAdviserId();
            CreateQuoteRequest request = ApiRequests.Quote(adviserId) with
            {
                Client = ApiRequests.Quote(adviserId).Client with { FullName = "" }
            };

            HttpResponseMessage response = await ApiRequests.PostQuoteAsync(_client, request, "invalid-1");

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            using JsonDocument problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            problem.RootElement.GetProperty("errors").TryGetProperty("client.fullName", out _).ShouldBeTrue();
        }

        [Fact]
        public async Task CreateQuote_CoverOutsideProductLimits_Returns400WithPricingCode()
        {
            HttpResponseMessage response = await ApiRequests.PostQuoteAsync(
                _client, ApiRequests.Quote(ApiRequests.NewAdviserId(), 50_000m), "limits-1");

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            (await ReadProblemCodeAsync(response)).ShouldBe("Pricing.CoverOutOfRange");
        }

        [Fact]
        public async Task StatusChanges_FollowTheLifecycleAndAreRecordedInHistory()
        {
            QuoteResponse created = await CreateQuoteAsync();

            HttpResponseMessage sent = await ApiRequests.PostStatusAsync(_client, created.Id, QuoteStatus.SentToClient, "Emailed");
            HttpResponseMessage accepted = await ApiRequests.PostStatusAsync(_client, created.Id, QuoteStatus.Accepted);

            sent.StatusCode.ShouldBe(HttpStatusCode.OK);
            accepted.StatusCode.ShouldBe(HttpStatusCode.OK);

            QuoteResponse quote = await _client.GetFromJsonAsync<QuoteResponse>($"{ApiRequests.QuotesPath}/{created.Id}", ApiRequests.Json)
                ?? throw new InvalidOperationException("Quote not returned.");
            quote.Status.ShouldBe(QuoteStatus.Accepted);
            quote.AllowedNextStatuses.ShouldBeEmpty();
            quote.StatusHistory.Select(change => change.ToStatus)
                .ShouldBe([QuoteStatus.Draft, QuoteStatus.SentToClient, QuoteStatus.Accepted]);
            quote.StatusHistory[1].Reason.ShouldBe("Emailed");
        }

        [Fact]
        public async Task StatusChange_NotAllowed_Returns409WithCode()
        {
            QuoteResponse created = await CreateQuoteAsync();

            HttpResponseMessage response = await ApiRequests.PostStatusAsync(_client, created.Id, QuoteStatus.Accepted);

            response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
            (await ReadProblemCodeAsync(response)).ShouldBe("Quote.InvalidStatusTransition");
        }

        [Fact]
        public async Task StatusChange_ConcurrentRequests_OnlyOneSucceeds()
        {
            QuoteResponse created = await CreateQuoteAsync();

            HttpResponseMessage[] responses = await Task.WhenAll(
                Enumerable.Range(0, 5).Select(_ => ApiRequests.PostStatusAsync(_client, created.Id, QuoteStatus.SentToClient)));

            responses.Count(response => response.StatusCode == HttpStatusCode.OK).ShouldBe(1);
            responses.Count(response => response.StatusCode == HttpStatusCode.Conflict).ShouldBe(4);

            QuoteResponse quote = await _client.GetFromJsonAsync<QuoteResponse>($"{ApiRequests.QuotesPath}/{created.Id}", ApiRequests.Json)
                ?? throw new InvalidOperationException("Quote not returned.");
            quote.StatusHistory.Count.ShouldBe(2);
        }

        [Fact]
        public async Task ReadSideProjection_MatchesTheAggregate()
        {
            QuoteResponse created = await CreateQuoteAsync();
            await ApiRequests.PostStatusAsync(_client, created.Id, QuoteStatus.SentToClient, "Emailed");
            await ApiRequests.PostStatusAsync(_client, created.Id, QuoteStatus.Declined, "Too expensive");

            using IServiceScope scope = _factory.Services.CreateScope();
            IQuoteRepository repository = scope.ServiceProvider.GetRequiredService<IQuoteRepository>();
            IQuoteReadService readService = scope.ServiceProvider.GetRequiredService<IQuoteReadService>();

            Quote aggregate = await repository.GetByIdAsync(created.Id, CancellationToken.None)
                ?? throw new InvalidOperationException("Quote not found through the repository.");
            QuoteResponse? projected = await readService.GetByIdAsync(created.Id, CancellationToken.None);

            projected.ShouldNotBeNull();
            projected.StatusHistory.Count.ShouldBe(3);
            projected.ShouldBeEquivalentTo(QuoteResponse.FromDomain(aggregate));
        }

        [Fact]
        public async Task GetQuote_Unknown_Returns404()
        {
            HttpResponseMessage response = await _client.GetAsync($"{ApiRequests.QuotesPath}/{Guid.CreateVersion7()}");

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            (await ReadProblemCodeAsync(response)).ShouldBe("Quote.NotFound");
        }

        [Fact]
        public async Task ListQuotes_FiltersByAdviserAndStatusAndPagesWithCursor()
        {
            string adviserId = ApiRequests.NewAdviserId();
            QuoteResponse first = await CreateQuoteAsync(adviserId, coverAmount: 500_000m);
            await CreateQuoteAsync(adviserId, coverAmount: 600_000m);
            await CreateQuoteAsync(adviserId, coverAmount: 700_000m);
            await ApiRequests.PostStatusAsync(_client, first.Id, QuoteStatus.SentToClient);

            PagedResponse<QuoteSummaryResponse>? page = await _client.GetFromJsonAsync<PagedResponse<QuoteSummaryResponse>>(
                $"{ApiRequests.QuotesPath}?adviserId={adviserId}&pageSize=2", ApiRequests.Json);
            PagedResponse<QuoteSummaryResponse>? nextPage = await _client.GetFromJsonAsync<PagedResponse<QuoteSummaryResponse>>(
                $"{ApiRequests.QuotesPath}?adviserId={adviserId}&pageSize=2&cursor={page!.NextCursor}", ApiRequests.Json);
            PagedResponse<QuoteSummaryResponse>? sent = await _client.GetFromJsonAsync<PagedResponse<QuoteSummaryResponse>>(
                $"{ApiRequests.QuotesPath}?adviserId={adviserId}&status=SentToClient", ApiRequests.Json);

            page.TotalCount.ShouldBe(3);
            page.Items.Select(item => item.CoverAmount).ShouldBe([700_000m, 600_000m]);
            page.NextCursor.ShouldNotBeNull();
            nextPage!.Items.ShouldHaveSingleItem().Id.ShouldBe(first.Id);
            nextPage.NextCursor.ShouldBeNull();
            nextPage.TotalCount.ShouldBeNull();
            sent!.Items.ShouldHaveSingleItem().Id.ShouldBe(first.Id);
        }

        [Fact]
        public async Task AdviserReference_IsStoredReturnedAndFilterable()
        {
            string adviserId = ApiRequests.NewAdviserId();
            string adviserReference = $"CRM-{Guid.NewGuid():N}"[..20];

            QuoteResponse tagged = await ReadAsync<QuoteResponse>(await ApiRequests.PostQuoteAsync(
                _client, ApiRequests.Quote(adviserId, adviserReference: adviserReference), Guid.NewGuid().ToString()));
            await CreateQuoteAsync(adviserId, coverAmount: 800_000m);

            QuoteResponse? fetched = await _client.GetFromJsonAsync<QuoteResponse>($"{ApiRequests.QuotesPath}/{tagged.Id}", ApiRequests.Json);
            PagedResponse<QuoteSummaryResponse>? filtered = await _client.GetFromJsonAsync<PagedResponse<QuoteSummaryResponse>>(
                $"{ApiRequests.QuotesPath}?adviserId={adviserId}&adviserReference={adviserReference}", ApiRequests.Json);

            tagged.AdviserReference.ShouldBe(adviserReference);
            tagged.ExternalReference.ShouldStartWith("Q-");
            fetched!.AdviserReference.ShouldBe(adviserReference);
            QuoteSummaryResponse match = filtered!.Items.ShouldHaveSingleItem();
            match.Id.ShouldBe(tagged.Id);
            match.AdviserReference.ShouldBe(adviserReference);
        }

        [Fact]
        public async Task ListQuotes_OpenOnlyForClientAndProduct_FindsTheClientsOpenQuotes()
        {
            string adviserId = ApiRequests.NewAdviserId();
            CreateQuoteRequest request = ApiRequests.Quote(adviserId);
            QuoteResponse open = await CreateQuoteAsync(adviserId, coverAmount: 500_000m);
            QuoteResponse accepted = await CreateQuoteAsync(adviserId, coverAmount: 600_000m);
            await ApiRequests.PostStatusAsync(_client, accepted.Id, QuoteStatus.SentToClient);
            await ApiRequests.PostStatusAsync(_client, accepted.Id, QuoteStatus.Accepted);
            await ApiRequests.PostQuoteAsync(
                _client,
                request with { Product = request.Product with { ProductCode = "SERIOUS_ILLNESS", CoverAmount = 500_000m, TermYears = 15 } },
                Guid.NewGuid().ToString());

            // Case and spaces in the name, and the product code's case, don't matter.
            string clientName = Uri.EscapeDataString($"  {request.Client.FullName.ToUpperInvariant()} ");
            PagedResponse<QuoteSummaryResponse>? matches = await _client.GetFromJsonAsync<PagedResponse<QuoteSummaryResponse>>(
                $"{ApiRequests.QuotesPath}?adviserId={adviserId}&clientName={clientName}"
                + $"&clientDateOfBirth={request.Client.DateOfBirth:yyyy-MM-dd}&productCode=term_life&openOnly=true",
                ApiRequests.Json);
            PagedResponse<QuoteSummaryResponse>? otherBirthDate = await _client.GetFromJsonAsync<PagedResponse<QuoteSummaryResponse>>(
                $"{ApiRequests.QuotesPath}?adviserId={adviserId}&clientName={clientName}"
                + $"&clientDateOfBirth={request.Client.DateOfBirth.AddDays(1):yyyy-MM-dd}&productCode=TERM_LIFE&openOnly=true",
                ApiRequests.Json);

            matches!.Items.ShouldHaveSingleItem().Id.ShouldBe(open.Id);
            otherBirthDate!.Items.ShouldBeEmpty();
        }

        [Fact]
        public async Task ListQuotes_PageSizeTooLarge_Returns400()
        {
            HttpResponseMessage response = await _client.GetAsync($"{ApiRequests.QuotesPath}?pageSize=500");

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task HealthReady_ReportsDatabaseHealthy()
        {
            HttpResponseMessage response = await _client.GetAsync("health/ready");

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            (await response.Content.ReadAsStringAsync()).ShouldContain("\"postgres\"");
        }

        private async Task<QuoteResponse> CreateQuoteAsync(string? adviserId = null, decimal coverAmount = 1_000_000m)
        {
            HttpResponseMessage response = await ApiRequests.PostQuoteAsync(
                _client,
                ApiRequests.Quote(adviserId ?? ApiRequests.NewAdviserId(), coverAmount),
                Guid.NewGuid().ToString());

            response.StatusCode.ShouldBe(HttpStatusCode.Created);

            return await ReadAsync<QuoteResponse>(response);
        }

        private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
        {
            return await response.Content.ReadFromJsonAsync<T>(ApiRequests.Json)
                ?? throw new InvalidOperationException($"Response body was not a {typeof(T).Name}.");
        }

        private static async Task<string?> ReadProblemCodeAsync(HttpResponseMessage response)
        {
            using JsonDocument problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

            return problem.RootElement.GetProperty("code").GetString();
        }
    }
}
