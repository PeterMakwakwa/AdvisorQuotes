using System.Diagnostics;
using System.Diagnostics.Metrics;
using Quotes.Domain.Common;
using Quotes.Domain.Enums;

namespace Quotes.Application.Observability
{
    /// <summary>
    /// Business metrics, complementing the traces: traces follow one request, metrics show totals and trends
    /// (quoting volume, how quotes progress, how often duplicates are caught, why requests are rejected).
    /// Every tag has a small, fixed set of values, so the metrics stay cheap to store.
    /// </summary>
    public sealed class QuotesMetrics
    {
        public const string MeterName = "Quotes.Application";

        private static readonly IReadOnlyList<double> PremiumBucketBoundaries = [100, 250, 500, 1_000, 2_500, 5_000, 10_000];

        private readonly Counter<long> _quotesCreated;

        private readonly Counter<long> _statusChanges;

        private readonly Counter<long> _duplicatesReplayed;

        private readonly Counter<long> _requestsRejected;

        private readonly Histogram<double> _monthlyPremium;

        public QuotesMetrics(IMeterFactory meterFactory)
        {
            Meter meter = meterFactory.Create(MeterName);

            _quotesCreated = meter.CreateCounter<long>(
                InstrumentNames.QuotesCreated,
                unit: "{quote}",
                description: "Quotes captured and priced.");

            _statusChanges = meter.CreateCounter<long>(
                InstrumentNames.StatusChanges,
                unit: "{change}",
                description: "Quote status changes, by from and to status.");

            _duplicatesReplayed = meter.CreateCounter<long>(
                InstrumentNames.DuplicatesReplayed,
                unit: "{request}",
                description: "Repeated submissions answered with the original quote instead of creating a new one.");

            _requestsRejected = meter.CreateCounter<long>(
                InstrumentNames.RequestsRejected,
                unit: "{request}",
                description: "Create and status-change requests rejected, by error code.");

            _monthlyPremium = meter.CreateHistogram(
                InstrumentNames.MonthlyPremium,
                unit: "{currency_unit}",
                description: "Monthly premium of newly created quotes.",
                tags: null,
                advice: new InstrumentAdvice<double> { HistogramBucketBoundaries = PremiumBucketBoundaries });
        }

        public void QuoteCreated(string productCode, decimal monthlyPremium, string currency)
        {
            _quotesCreated.Add(1, new TagList { { QuotesTelemetry.TagNames.ProductCode, productCode } });

            _monthlyPremium.Record(
                (double)monthlyPremium,
                new TagList
                {
                    { QuotesTelemetry.TagNames.ProductCode, productCode },
                    { QuotesTelemetry.TagNames.Currency, currency }
                });
        }

        public void StatusChanged(QuoteStatus fromStatus, QuoteStatus toStatus)
        {
            _statusChanges.Add(
                1,
                new TagList
                {
                    { QuotesTelemetry.TagNames.StatusFrom, fromStatus.ToString() },
                    { QuotesTelemetry.TagNames.StatusTo, toStatus.ToString() }
                });
        }

        public void DuplicateReplayed(string keySource)
        {
            _duplicatesReplayed.Add(1, new TagList { { QuotesTelemetry.TagNames.IdempotencyKeySource, keySource } });
        }

        public void RequestRejected(string operation, Error error)
        {
            _requestsRejected.Add(
                1,
                new TagList
                {
                    { QuotesTelemetry.TagNames.Operation, operation },
                    { QuotesTelemetry.TagNames.ErrorCode, error.Code },
                    { QuotesTelemetry.TagNames.ErrorType, error.Type.ToString() }
                });
        }

        public static class InstrumentNames
        {
            public const string QuotesCreated = "quotes.created";

            public const string StatusChanges = "quotes.status_changes";

            public const string DuplicatesReplayed = "quotes.duplicates_replayed";

            public const string RequestsRejected = "quotes.rejected";

            public const string MonthlyPremium = "quotes.monthly_premium";
        }

        public static class Operations
        {
            public const string CreateQuote = "create_quote";

            public const string ChangeStatus = "change_status";
        }

        public static class KeySources
        {
            public const string Client = "client";

            public const string Fingerprint = "fingerprint";
        }
    }
}
