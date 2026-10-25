using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Quotes.Application.Exceptions;
using Quotes.Application.Handlers;
using Quotes.Application.Idempotency;
using Quotes.Application.Models.Commands;
using Quotes.Application.Models.Request.V1;
using Quotes.Application.Models.Results;
using Quotes.Application.Observability;
using Quotes.Application.Pricing;
using Quotes.Application.Validators;
using Quotes.Domain.Commands;
using Quotes.Domain.Common;
using Quotes.Domain.Entities;
using Quotes.Domain.Enums;
using Quotes.Domain.ValueObjects;
using Quotes.UnitTests.Fakes;
using Quotes.UnitTests.TestData;
using Shouldly;

namespace Quotes.UnitTests.Application
{
    public sealed class CreateQuoteHandlerTests : IDisposable
    {
        private const string IdempotencyKey = "4f5b2c1e-key";

        private readonly FakeTimeProvider _time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 24, 9, 0, 0, TimeSpan.Zero));

        private readonly InMemoryQuoteStore _store = new InMemoryQuoteStore();

        private readonly TestMetrics _metrics = new TestMetrics();

        private readonly InstrumentedCreateQuoteHandler _handler;

        public CreateQuoteHandlerTests()
        {
            // The same chain as production: the instrumented decorator around the business handler.
            CreateQuoteHandler handler = new CreateQuoteHandler(
                new CreateQuoteRequestValidator(_time),
                new IdempotencyGuard(_store, Options.Create(new IdempotencyOptions()), _time),
                new LocalPricingService(new FakeRateTableProvider(), _time),
                new SequentialReferenceGenerator(),
                _store,
                _store,
                _time);

            _handler = new InstrumentedCreateQuoteHandler(handler, _metrics.Metrics, NullLogger<InstrumentedCreateQuoteHandler>.Instance);
        }

        public void Dispose()
        {
            _metrics.Dispose();
        }

        [Fact]
        public async Task Handle_AdviserReference_IsTrimmedAndReturned()
        {
            Result<CreateQuoteResult> result = await HandleAsync(
                TestRequests.CreateQuote(adviserReference: "  CRM-2026-0042 "),
                IdempotencyKey);

            result.Value.Quote.AdviserReference.ShouldBe("CRM-2026-0042");
            result.Value.Quote.ExternalReference.ShouldBe("Q-2026-000001");
        }

        [Fact]
        public async Task Handle_AdviserReferenceTooLong_ReturnsFieldError()
        {
            Result<CreateQuoteResult> result = await HandleAsync(
                TestRequests.CreateQuote(adviserReference: new string('r', 51)),
                IdempotencyKey);

            result.Error!.ValidationErrors!.ShouldContainKey("adviserReference");
        }

        [Fact]
        public async Task Handle_NewQuote_RecordsQuoteCreatedAndPremiumMetrics()
        {
            using MetricCollector<long> created = _metrics.Collect<long>(QuotesMetrics.InstrumentNames.QuotesCreated);
            using MetricCollector<double> premium = _metrics.Collect<double>(QuotesMetrics.InstrumentNames.MonthlyPremium);

            await HandleAsync(TestRequests.CreateQuote(), IdempotencyKey);

            CollectedMeasurement<long> measurement = created.GetMeasurementSnapshot().ShouldHaveSingleItem();
            measurement.Value.ShouldBe(1);
            measurement.Tags[QuotesTelemetry.TagNames.ProductCode].ShouldBe(TestRates.TermLife);
            CollectedMeasurement<double> premiumMeasurement = premium.GetMeasurementSnapshot().ShouldHaveSingleItem();
            premiumMeasurement.Value.ShouldBe(300.0);
            premiumMeasurement.Tags[QuotesTelemetry.TagNames.Currency].ShouldBe("ZAR");
        }

        [Fact]
        public async Task Handle_Replay_RecordsDuplicateInsteadOfASecondCreation()
        {
            using MetricCollector<long> created = _metrics.Collect<long>(QuotesMetrics.InstrumentNames.QuotesCreated);
            using MetricCollector<long> duplicates = _metrics.Collect<long>(QuotesMetrics.InstrumentNames.DuplicatesReplayed);

            await HandleAsync(TestRequests.CreateQuote(), IdempotencyKey);
            await HandleAsync(TestRequests.CreateQuote(), IdempotencyKey);
            await HandleAsync(TestRequests.CreateQuote(coverAmount: 2_000_000m), idempotencyKey: null);
            await HandleAsync(TestRequests.CreateQuote(coverAmount: 2_000_000m), idempotencyKey: null);

            created.GetMeasurementSnapshot().Count.ShouldBe(2);
            IReadOnlyList<CollectedMeasurement<long>> replays = duplicates.GetMeasurementSnapshot();
            replays.Count.ShouldBe(2);
            replays[0].Tags[QuotesTelemetry.TagNames.IdempotencyKeySource].ShouldBe(QuotesMetrics.KeySources.Client);
            replays[1].Tags[QuotesTelemetry.TagNames.IdempotencyKeySource].ShouldBe(QuotesMetrics.KeySources.Fingerprint);
        }

        [Fact]
        public async Task Handle_Rejected_RecordsRejectionWithErrorCode()
        {
            using MetricCollector<long> created = _metrics.Collect<long>(QuotesMetrics.InstrumentNames.QuotesCreated);
            using MetricCollector<long> rejected = _metrics.Collect<long>(QuotesMetrics.InstrumentNames.RequestsRejected);

            await HandleAsync(TestRequests.CreateQuote(coverAmount: 50_000m), IdempotencyKey);

            created.GetMeasurementSnapshot().ShouldBeEmpty();
            CollectedMeasurement<long> rejection = rejected.GetMeasurementSnapshot().ShouldHaveSingleItem();
            rejection.Tags[QuotesTelemetry.TagNames.Operation].ShouldBe(QuotesMetrics.Operations.CreateQuote);
            rejection.Tags[QuotesTelemetry.TagNames.ErrorCode].ShouldBe("Pricing.CoverOutOfRange");
            rejection.Tags[QuotesTelemetry.TagNames.ErrorType].ShouldBe(nameof(ErrorType.Validation));
        }

        [Fact]
        public async Task Handle_ValidRequest_CreatesPricedDraftQuote()
        {
            Result<CreateQuoteResult> result = await HandleAsync(TestRequests.CreateQuote(), IdempotencyKey);

            result.IsSuccess.ShouldBeTrue();
            result.Value.IsReplay.ShouldBeFalse();
            result.Value.Quote.Status.ShouldBe(QuoteStatus.Draft);
            result.Value.Quote.ExternalReference.ShouldBe("Q-2026-000001");
            result.Value.Quote.Premium.MonthlyAmount.ShouldBe(300.00m);
            result.Value.Quote.Premium.PricingVersion.ShouldBe("2026.1");
            _store.Quotes.ShouldHaveSingleItem();
            _store.IdempotencyRecords.ShouldHaveSingleItem();
        }

        [Fact]
        public async Task Handle_SameKeyAndBody_ReturnsOriginalQuoteWithoutCreatingAnother()
        {
            Result<CreateQuoteResult> first = await HandleAsync(TestRequests.CreateQuote(), IdempotencyKey);
            Result<CreateQuoteResult> second = await HandleAsync(TestRequests.CreateQuote(), IdempotencyKey);

            second.Value.IsReplay.ShouldBeTrue();
            second.Value.Quote.Id.ShouldBe(first.Value.Quote.Id);
            _store.Quotes.ShouldHaveSingleItem();
        }

        [Fact]
        public async Task Handle_SameKeyWithHarmlessDifferences_IsReplayedNotRejected()
        {
            Result<CreateQuoteResult> first = await HandleAsync(TestRequests.CreateQuote(), IdempotencyKey);
            CreateQuoteRequest retry = TestRequests.CreateQuote(notes: "   ") with
            {
                AdviserId = $"  {TestRequests.AdviserId} ",
                Client = TestRequests.CreateQuote().Client with { FullName = " Thandi Nkosi  " },
                Product = TestRequests.CreateQuote().Product with { ProductCode = " term_life " }
            };

            Result<CreateQuoteResult> second = await HandleAsync(retry, IdempotencyKey);

            second.Value.IsReplay.ShouldBeTrue();
            second.Value.Quote.Id.ShouldBe(first.Value.Quote.Id);
            _store.Quotes.ShouldHaveSingleItem();
        }

        [Fact]
        public async Task Handle_NoKey_HarmlessDifferences_StillCaughtAsDoubleSubmit()
        {
            await HandleAsync(TestRequests.CreateQuote(), idempotencyKey: null);
            CreateQuoteRequest retry = TestRequests.CreateQuote() with
            {
                Product = TestRequests.CreateQuote().Product with { ProductCode = "Term_Life" }
            };

            Result<CreateQuoteResult> second = await HandleAsync(retry, idempotencyKey: null);

            second.Value.IsReplay.ShouldBeTrue();
            _store.Quotes.ShouldHaveSingleItem();
        }

        [Fact]
        public async Task Handle_SameKeyDifferentBody_IsRejected()
        {
            await HandleAsync(TestRequests.CreateQuote(), IdempotencyKey);

            Result<CreateQuoteResult> result = await HandleAsync(TestRequests.CreateQuote(coverAmount: 2_000_000m), IdempotencyKey);

            result.Error!.Code.ShouldBe("Idempotency.KeyReused");
            result.Error.Type.ShouldBe(ErrorType.Unprocessable);
            _store.Quotes.ShouldHaveSingleItem();
        }

        [Fact]
        public async Task Handle_NoKey_IdenticalSubmissionWithinWindow_IsReplayed()
        {
            Result<CreateQuoteResult> first = await HandleAsync(TestRequests.CreateQuote(), idempotencyKey: null);
            _time.Advance(TimeSpan.FromMinutes(2));

            Result<CreateQuoteResult> second = await HandleAsync(TestRequests.CreateQuote(), idempotencyKey: null);

            second.Value.IsReplay.ShouldBeTrue();
            second.Value.Quote.Id.ShouldBe(first.Value.Quote.Id);
            _store.Quotes.ShouldHaveSingleItem();
        }

        [Fact]
        public async Task Handle_NoKey_IdenticalSubmissionAfterWindow_CreatesNewQuote()
        {
            await HandleAsync(TestRequests.CreateQuote(), idempotencyKey: null);
            _time.Advance(TimeSpan.FromMinutes(11));

            Result<CreateQuoteResult> second = await HandleAsync(TestRequests.CreateQuote(), idempotencyKey: null);

            second.Value.IsReplay.ShouldBeFalse();
            _store.Quotes.Count.ShouldBe(2);
            _store.IdempotencyRecords.ShouldHaveSingleItem().QuoteId.ShouldBe(second.Value.Quote.Id);
        }

        [Fact]
        public async Task Handle_InvalidRequest_ReturnsFieldErrorsAndSavesNothing()
        {
            CreateQuoteRequest request = TestRequests.CreateQuote() with { AdviserId = "" };

            Result<CreateQuoteResult> result = await HandleAsync(request, IdempotencyKey);

            result.Error!.Type.ShouldBe(ErrorType.Validation);
            result.Error.ValidationErrors!.ShouldContainKey("adviserId");
            _store.SaveCount.ShouldBe(0);
        }

        [Fact]
        public async Task Handle_KeyTooLong_IsRejected()
        {
            Result<CreateQuoteResult> result = await HandleAsync(TestRequests.CreateQuote(), new string('k', 101));

            result.Error!.Code.ShouldBe("Idempotency.KeyTooLong");
        }

        [Fact]
        public async Task Handle_PricingRejectsCover_ReturnsPricingErrorAndSavesNothing()
        {
            Result<CreateQuoteResult> result = await HandleAsync(TestRequests.CreateQuote(coverAmount: 50_000m), IdempotencyKey);

            result.Error!.Code.ShouldBe("Pricing.CoverOutOfRange");
            _store.SaveCount.ShouldBe(0);
            _store.Quotes.ShouldBeEmpty();
        }

        [Fact]
        public async Task Handle_ConcurrentDuplicateWinsRace_ReturnsTheWinningQuote()
        {
            CreateQuoteRequest request = TestRequests.CreateQuote();
            Quote winner = Quote.Create(
                new CreateQuote
                {
                    ExternalReference = "Q-2026-999999",
                    AdviserId = TestRequests.AdviserId,
                    Client = request.Client.BuildClientDetails(),
                    Product = request.Product.BuildProductDetails(),
                    MonthlyPremium = Money.Of(300m, "ZAR"),
                    PricingVersion = "2026.1"
                },
                _time.GetUtcNow()).Value;

            // While this request is saving, a concurrent identical request commits first
            _store.FailNextSaveWith(
                new DuplicateIdempotencyKeyException("duplicate key", new InvalidOperationException()),
                () =>
                {
                    _store.Quotes.Add(winner);
                    _store.IdempotencyRecords.Add(IdempotencyRecord.Create(
                        TestRequests.AdviserId,
                        IdempotencyKey,
                        RequestHasher.Compute(request.Normalize()),
                        winner.Id,
                        _time.GetUtcNow(),
                        TimeSpan.FromHours(24)));
                });

            Result<CreateQuoteResult> result = await HandleAsync(request, IdempotencyKey);

            result.Value.IsReplay.ShouldBeTrue();
            result.Value.Quote.Id.ShouldBe(winner.Id);
            _store.Quotes.ShouldHaveSingleItem();
        }

        private Task<Result<CreateQuoteResult>> HandleAsync(CreateQuoteRequest request, string? idempotencyKey)
        {
            return _handler.HandleAsync(
                new CreateQuoteCommand { IdempotencyKey = idempotencyKey, Request = request },
                CancellationToken.None);
        }
    }
}
