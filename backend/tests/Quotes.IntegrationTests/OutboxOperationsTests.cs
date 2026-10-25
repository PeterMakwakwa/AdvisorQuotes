using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Quotes.Application.Models.Response.V1;
using Quotes.Infrastructure.Maintenance;
using Quotes.Infrastructure.Messaging;
using Quotes.IntegrationTests.Infrastructure;
using Shouldly;

namespace Quotes.IntegrationTests
{
    /// <summary>
    /// What operators rely on: the backlog figures and gauges, the outbox health check, and batched cleanup.
    /// </summary>
    [Collection(ApiCollection.Name)]
    public class OutboxOperationsTests
    {
        private readonly QuotesApiFactory _factory;

        private readonly HttpClient _client;

        public OutboxOperationsTests(QuotesApiFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Backlog_CountsPendingAndDeadLetteredMessages()
        {
            await _factory.DrainOutboxAsync();
            QuoteResponse pending = await CreateQuoteAsync();
            QuoteResponse deadLettered = await CreateQuoteAsync();
            await GiveUpOnAsync(deadLettered.Id);

            try
            {
                OutboxBacklog backlog = await ReadBacklogAsync();

                backlog.Pending.ShouldBe(1);
                backlog.DeadLettered.ShouldBe(1);
                backlog.OldestPendingAge.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero);
                (await _factory.GetOutboxRowsAsync(pending.Id)).ShouldHaveSingleItem().ProcessedAt.ShouldBeNull();
            }
            finally
            {
                await RecoverAsync(deadLettered.Id);
            }
        }

        [Fact]
        public async Task HealthEndpoint_ReportsOutboxDegraded_WhileAMessageIsDeadLettered_ButReadinessStaysHealthy()
        {
            await _factory.DrainOutboxAsync();
            QuoteResponse quote = await CreateQuoteAsync();
            await GiveUpOnAsync(quote.Id);

            try
            {
                HttpResponseMessage health = await _client.GetAsync("health");
                HttpResponseMessage ready = await _client.GetAsync("health/ready");

                health.StatusCode.ShouldBe(HttpStatusCode.OK);
                JsonElement outbox = await FindCheckAsync(health, "outbox");
                outbox.GetProperty("status").GetString().ShouldBe("Degraded");
                outbox.GetProperty("data").GetProperty("deadLettered").GetInt32().ShouldBe(1);
                outbox.GetProperty("description").GetString().ShouldNotBeNull().ShouldContain("gave up");
                (await ready.Content.ReadAsStringAsync()).ShouldNotContain("outbox");
                ready.StatusCode.ShouldBe(HttpStatusCode.OK);
            }
            finally
            {
                await RecoverAsync(quote.Id);
            }

            JsonElement recovered = await FindCheckAsync(await _client.GetAsync("health"), "outbox");
            recovered.GetProperty("status").GetString().ShouldBe("Healthy");
        }

        [Fact]
        public async Task BacklogGauges_ReportTheLastBacklogRead()
        {
            await _factory.DrainOutboxAsync();
            QuoteResponse quote = await CreateQuoteAsync();
            IMeterFactory meterFactory = _factory.Services.GetRequiredService<IMeterFactory>();
            using MetricCollector<int> pending = new MetricCollector<int>(
                meterFactory, MessagingTelemetry.SourceName, MessagingTelemetry.InstrumentNames.Pending);

            // The health check refreshes the gauges (as does each dispatcher run).
            await _client.GetAsync("health");
            pending.RecordObservableInstruments();

            pending.LastMeasurement.ShouldNotBeNull().Value.ShouldBeGreaterThanOrEqualTo(1);
            (await _factory.GetOutboxRowsAsync(quote.Id)).ShouldHaveSingleItem().ProcessedAt.ShouldBeNull();
            await _factory.DrainOutboxAsync();
        }

        [Fact]
        public async Task Maintenance_DeletesALargeBacklogInSeveralSmallBatches()
        {
            List<QuoteResponse> quotes = [];

            for (int index = 0; index < 5; index++)
            {
                quotes.Add(await CreateQuoteAsync());
            }

            await _factory.DrainOutboxAsync();

            foreach (QuoteResponse quote in quotes)
            {
                await _factory.ExecuteSqlAsync(
                    "UPDATE outbox_messages SET processed_at = now() - interval '30 days' WHERE payload->>'quoteId' = @quoteId",
                    ("quoteId", quote.Id.ToString()));
            }

            await using WebApplicationFactory<Program> smallBatches = _factory.WithWebHostBuilder(builder =>
                builder.UseSetting("Maintenance:DeleteBatchSize", "2"));
            using IServiceScope scope = smallBatches.Services.CreateScope();

            MaintenanceResult result = await scope.ServiceProvider.GetRequiredService<DatabaseMaintenance>().RunAsync(CancellationToken.None);

            result.OutboxMessagesDeleted.ShouldBe(5);

            // 5 messages at 2 per statement: 2 + 2 + 1, plus one (empty) statement for idempotency keys.
            result.DeleteStatements.ShouldBe(4);

            foreach (QuoteResponse quote in quotes)
            {
                (await _factory.GetOutboxRowsAsync(quote.Id)).ShouldBeEmpty();
            }
        }

        private async Task<QuoteResponse> CreateQuoteAsync()
        {
            HttpResponseMessage response = await ApiRequests.PostQuoteAsync(
                _client, ApiRequests.Quote(ApiRequests.NewAdviserId()), Guid.NewGuid().ToString());

            response.StatusCode.ShouldBe(HttpStatusCode.Created);

            return (await response.Content.ReadFromJsonAsync<QuoteResponse>(ApiRequests.Json))!;
        }

        private Task<OutboxBacklog> ReadBacklogAsync()
        {
            return _factory.InScopeAsync(services => services.GetRequiredService<OutboxBacklogReader>().ReadAsync(CancellationToken.None));
        }

        /// <summary>
        /// Simulates a message that used up every attempt.
        /// </summary>
        private Task<int> GiveUpOnAsync(Guid quoteId)
        {
            return _factory.ExecuteSqlAsync(
                "UPDATE outbox_messages SET attempts = 20, last_error = 'simulated' WHERE payload->>'quoteId' = @quoteId",
                ("quoteId", quoteId.ToString()));
        }

        /// <summary>
        /// The operator's re-drive after fixing the cause: reset the attempts, and the dispatcher publishes it.
        /// </summary>
        private async Task RecoverAsync(Guid quoteId)
        {
            await _factory.ExecuteSqlAsync(
                "UPDATE outbox_messages SET attempts = 0, last_error = NULL, next_attempt_at = NULL WHERE payload->>'quoteId' = @quoteId",
                ("quoteId", quoteId.ToString()));
            await _factory.DrainOutboxAsync();
        }

        private static async Task<JsonElement> FindCheckAsync(HttpResponseMessage response, string name)
        {
            using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

            return document.RootElement.GetProperty("checks").EnumerateArray()
                .Single(check => check.GetProperty("name").GetString() == name)
                .Clone();
        }
    }
}
