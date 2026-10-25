using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Quotes.Application.Models.Response.V1;
using Quotes.Application.Observability;
using Quotes.Domain.Enums;
using Quotes.IntegrationTests.Infrastructure;
using Shouldly;

namespace Quotes.IntegrationTests
{
    /// <summary>
    /// Proves the business metrics are recorded by real HTTP requests through the running API. Tests in the
    /// collection run one at a time, and collectors are scoped to this host's meter factory.
    /// </summary>
    [Collection(ApiCollection.Name)]
    public class QuoteMetricsTests
    {
        private readonly HttpClient _client;

        private readonly IMeterFactory _meterFactory;

        public QuoteMetricsTests(QuotesApiFactory factory)
        {
            _client = factory.CreateClient();
            _meterFactory = factory.Services.GetRequiredService<IMeterFactory>();
        }

        [Fact]
        public async Task QuoteLifecycle_RecordsBusinessMetrics()
        {
            using MetricCollector<long> created = Collect<long>(QuotesMetrics.InstrumentNames.QuotesCreated);
            using MetricCollector<long> duplicates = Collect<long>(QuotesMetrics.InstrumentNames.DuplicatesReplayed);
            using MetricCollector<long> changes = Collect<long>(QuotesMetrics.InstrumentNames.StatusChanges);
            using MetricCollector<long> rejected = Collect<long>(QuotesMetrics.InstrumentNames.RequestsRejected);
            using MetricCollector<double> premium = Collect<double>(QuotesMetrics.InstrumentNames.MonthlyPremium);
            string adviserId = ApiRequests.NewAdviserId();

            HttpResponseMessage first = await ApiRequests.PostQuoteAsync(_client, ApiRequests.Quote(adviserId), "metrics-1");
            await ApiRequests.PostQuoteAsync(_client, ApiRequests.Quote(adviserId), "metrics-1");
            QuoteResponse quote = await first.Content.ReadFromJsonAsync<QuoteResponse>(ApiRequests.Json)
                ?? throw new InvalidOperationException("Quote not returned.");
            HttpResponseMessage sent = await ApiRequests.PostStatusAsync(_client, quote.Id, QuoteStatus.SentToClient);
            HttpResponseMessage illegal = await ApiRequests.PostStatusAsync(_client, quote.Id, QuoteStatus.Draft);

            sent.StatusCode.ShouldBe(HttpStatusCode.OK);
            illegal.StatusCode.ShouldBe(HttpStatusCode.Conflict);

            created.GetMeasurementSnapshot().ShouldHaveSingleItem()
                .Tags[QuotesTelemetry.TagNames.ProductCode].ShouldBe("TERM_LIFE");
            premium.GetMeasurementSnapshot().ShouldHaveSingleItem().Value.ShouldBe(300.0);
            duplicates.GetMeasurementSnapshot().ShouldHaveSingleItem()
                .Tags[QuotesTelemetry.TagNames.IdempotencyKeySource].ShouldBe(QuotesMetrics.KeySources.Client);

            CollectedMeasurement<long> change = changes.GetMeasurementSnapshot().ShouldHaveSingleItem();
            change.Tags[QuotesTelemetry.TagNames.StatusFrom].ShouldBe(nameof(QuoteStatus.Draft));
            change.Tags[QuotesTelemetry.TagNames.StatusTo].ShouldBe(nameof(QuoteStatus.SentToClient));

            CollectedMeasurement<long> rejection = rejected.GetMeasurementSnapshot().ShouldHaveSingleItem();
            rejection.Tags[QuotesTelemetry.TagNames.Operation].ShouldBe(QuotesMetrics.Operations.ChangeStatus);
            rejection.Tags[QuotesTelemetry.TagNames.ErrorCode].ShouldBe("Quote.InvalidStatusTransition");
        }

        private MetricCollector<T> Collect<T>(string instrumentName)
            where T : struct
        {
            return new MetricCollector<T>(_meterFactory, QuotesMetrics.MeterName, instrumentName);
        }
    }
}
