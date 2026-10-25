using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Quotes.Application.Exceptions;
using Quotes.Application.Handlers;
using Quotes.Application.Models.Commands;
using Quotes.Application.Models.Request.V1;
using Quotes.Application.Models.Response.V1;
using Quotes.Application.Observability;
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
    public sealed class ChangeQuoteStatusHandlerTests : IDisposable
    {
        private readonly FakeTimeProvider _time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 24, 9, 0, 0, TimeSpan.Zero));

        private readonly InMemoryQuoteStore _store = new InMemoryQuoteStore();

        private readonly TestMetrics _metrics = new TestMetrics();

        private readonly InstrumentedChangeQuoteStatusHandler _handler;

        public ChangeQuoteStatusHandlerTests()
        {
            // The same chain as production: the instrumented decorator around the business handler.
            ChangeQuoteStatusHandler handler = new ChangeQuoteStatusHandler(
                new ChangeQuoteStatusRequestValidator(),
                _store,
                _store,
                _time);

            _handler = new InstrumentedChangeQuoteStatusHandler(handler, _metrics.Metrics, NullLogger<InstrumentedChangeQuoteStatusHandler>.Instance);
        }

        public void Dispose()
        {
            _metrics.Dispose();
        }

        [Fact]
        public async Task Handle_AllowedTransition_RecordsStatusChangeMetric()
        {
            Quote quote = SeedDraftQuote();
            using MetricCollector<long> changes = _metrics.Collect<long>(QuotesMetrics.InstrumentNames.StatusChanges);

            await HandleAsync(quote.Id, QuoteStatus.SentToClient);

            CollectedMeasurement<long> change = changes.GetMeasurementSnapshot().ShouldHaveSingleItem();
            change.Tags[QuotesTelemetry.TagNames.StatusFrom].ShouldBe(nameof(QuoteStatus.Draft));
            change.Tags[QuotesTelemetry.TagNames.StatusTo].ShouldBe(nameof(QuoteStatus.SentToClient));
        }

        [Fact]
        public async Task Handle_TransitionNotAllowed_RecordsRejectionNotChange()
        {
            Quote quote = SeedDraftQuote();
            using MetricCollector<long> changes = _metrics.Collect<long>(QuotesMetrics.InstrumentNames.StatusChanges);
            using MetricCollector<long> rejected = _metrics.Collect<long>(QuotesMetrics.InstrumentNames.RequestsRejected);

            await HandleAsync(quote.Id, QuoteStatus.Accepted);

            changes.GetMeasurementSnapshot().ShouldBeEmpty();
            CollectedMeasurement<long> rejection = rejected.GetMeasurementSnapshot().ShouldHaveSingleItem();
            rejection.Tags[QuotesTelemetry.TagNames.Operation].ShouldBe(QuotesMetrics.Operations.ChangeStatus);
            rejection.Tags[QuotesTelemetry.TagNames.ErrorCode].ShouldBe("Quote.InvalidStatusTransition");
        }

        [Fact]
        public async Task Handle_UnknownQuote_ReturnsNotFound()
        {
            Result<QuoteResponse> result = await HandleAsync(Guid.CreateVersion7(), QuoteStatus.SentToClient);

            result.Error!.Type.ShouldBe(ErrorType.NotFound);
        }

        [Fact]
        public async Task Handle_AllowedTransition_SavesAndReturnsUpdatedQuote()
        {
            Quote quote = SeedDraftQuote();
            _time.Advance(TimeSpan.FromMinutes(5));

            Result<QuoteResponse> result = await HandleAsync(quote.Id, QuoteStatus.SentToClient);

            result.IsSuccess.ShouldBeTrue();
            result.Value.Status.ShouldBe(QuoteStatus.SentToClient);
            result.Value.AllowedNextStatuses.ShouldBe([QuoteStatus.Accepted, QuoteStatus.Declined]);
            result.Value.StatusHistory.Count.ShouldBe(2);
            result.Value.StatusHistory[1].FromStatus.ShouldBe(QuoteStatus.Draft);
            _store.SaveCount.ShouldBe(1);
        }

        [Fact]
        public async Task Handle_TransitionNotAllowed_ReturnsConflictAndSavesNothing()
        {
            Quote quote = SeedDraftQuote();

            Result<QuoteResponse> result = await HandleAsync(quote.Id, QuoteStatus.Accepted);

            result.Error!.Code.ShouldBe("Quote.InvalidStatusTransition");
            result.Error.Type.ShouldBe(ErrorType.Conflict);
            _store.SaveCount.ShouldBe(0);
        }

        [Fact]
        public async Task Handle_ConcurrentUpdate_ReturnsConflict()
        {
            Quote quote = SeedDraftQuote();
            _store.FailNextSaveWith(new ConcurrencyConflictException("row changed", new InvalidOperationException()));

            Result<QuoteResponse> result = await HandleAsync(quote.Id, QuoteStatus.SentToClient);

            result.Error!.Code.ShouldBe("Quote.ConcurrentUpdate");
        }

        [Fact]
        public async Task Handle_MissingChangedBy_ReturnsValidationError()
        {
            Quote quote = SeedDraftQuote();

            Result<QuoteResponse> result = await HandleAsync(quote.Id, QuoteStatus.SentToClient, changedBy: " ");

            result.Error!.ValidationErrors!.ShouldContainKey("changedBy");
        }

        private Quote SeedDraftQuote()
        {
            Quote quote = Quote.Create(
                new CreateQuote
                {
                    ExternalReference = "Q-2026-000001",
                    AdviserId = TestRequests.AdviserId,
                    Client = new ClientDetails { FullName = "Thandi Nkosi", DateOfBirth = new DateOnly(1991, 1, 1), IsSmoker = false },
                    Product = new ProductDetails { ProductCode = TestRates.TermLife, CoverAmount = 1_000_000m, TermYears = 20 },
                    MonthlyPremium = Money.Of(300m, "ZAR"),
                    PricingVersion = "2026.1"
                },
                _time.GetUtcNow()).Value;

            _store.Quotes.Add(quote);

            return quote;
        }

        private Task<Result<QuoteResponse>> HandleAsync(Guid quoteId, QuoteStatus newStatus, string changedBy = TestRequests.AdviserId)
        {
            return _handler.HandleAsync(
                new ChangeQuoteStatusCommand
                {
                    QuoteId = quoteId,
                    Request = new ChangeQuoteStatusRequest { NewStatus = newStatus, ChangedBy = changedBy }
                },
                CancellationToken.None);
        }
    }
}
