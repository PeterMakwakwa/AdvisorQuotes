using Quotes.Application.Handlers;
using Quotes.Application.Models.Queries;
using Quotes.Application.Models.Request.V1;
using Quotes.Application.Models.Response.V1;
using Quotes.Application.Validators;
using Quotes.Domain.Common;
using Quotes.Domain.Entities;
using Quotes.Domain.Enums;
using Quotes.UnitTests.Fakes;
using Quotes.UnitTests.TestData;
using Shouldly;

namespace Quotes.UnitTests.Application
{
    public class QueryHandlerTests
    {
        private static readonly DateTimeOffset Now = new DateTimeOffset(2026, 10, 24, 9, 0, 0, TimeSpan.Zero);

        private readonly InMemoryQuoteStore _store = new InMemoryQuoteStore();

        [Fact]
        public void ListQuotesRequest_BuildQuery_CopiesEveryFilterAndPagingValue()
        {
            ListQuotesRequest request = new ListQuotesRequest
            {
                AdviserId = "ADV-001",
                AdviserReference = "CRM-1",
                Status = QuoteStatus.SentToClient,
                Cursor = "abc",
                PageSize = 50
            };

            ListQuotesQuery query = request.BuildQuery();

            query.ShouldBe(new ListQuotesQuery
            {
                AdviserId = "ADV-001",
                AdviserReference = "CRM-1",
                Status = QuoteStatus.SentToClient,
                Cursor = "abc",
                PageSize = 50
            });
        }

        [Fact]
        public void ListQuotesRequest_Defaults_MatchTheQueryDefaults()
        {
            new ListQuotesRequest().BuildQuery().ShouldBe(new ListQuotesQuery());
        }

        [Fact]
        public async Task ListQuotes_FiltersNewestFirstAndReturnsACursorForTheNextPage()
        {
            _store.Quotes.Add(TestQuotes.Draft(Now, externalReference: "Q-2026-000001"));
            _store.Quotes.Add(TestQuotes.Draft(Now.AddMinutes(1), externalReference: "Q-2026-000002", adviserReference: "CRM-1"));
            _store.Quotes.Add(TestQuotes.Draft(Now.AddMinutes(2), externalReference: "Q-2026-000003", adviserReference: "CRM-1"));
            _store.Quotes.Add(TestQuotes.Draft(Now.AddMinutes(3), adviserId: "ADV-OTHER", externalReference: "Q-2026-000004"));
            ListQuotesHandler handler = CreateListHandler();

            Result<PagedResponse<QuoteSummaryResponse>> first = await handler.HandleAsync(
                new ListQuotesQuery { AdviserId = TestRequests.AdviserId, AdviserReference = "CRM-1", PageSize = 1 },
                CancellationToken.None);
            Result<PagedResponse<QuoteSummaryResponse>> second = await handler.HandleAsync(
                new ListQuotesQuery { AdviserId = TestRequests.AdviserId, AdviserReference = "CRM-1", PageSize = 1, Cursor = first.Value.NextCursor },
                CancellationToken.None);

            first.Value.TotalCount.ShouldBe(2);
            first.Value.Items.ShouldHaveSingleItem().ExternalReference.ShouldBe("Q-2026-000003");
            first.Value.NextCursor.ShouldNotBeNull();
            second.Value.Items.ShouldHaveSingleItem().ExternalReference.ShouldBe("Q-2026-000002");
            second.Value.NextCursor.ShouldBeNull();
        }

        [Fact]
        public async Task ListQuotes_WalkingEveryPage_ReturnsEachQuoteExactlyOnce()
        {
            // Several quotes share a timestamp, so the id tie-breaker decides the order between them.
            for (int index = 0; index < 7; index++)
            {
                _store.Quotes.Add(TestQuotes.Draft(Now.AddMinutes(index / 3), externalReference: $"Q-2026-{index:D6}"));
            }

            ListQuotesHandler handler = CreateListHandler();
            List<string> seen = [];
            string? cursor = null;

            do
            {
                Result<PagedResponse<QuoteSummaryResponse>> page = await handler.HandleAsync(
                    new ListQuotesQuery { PageSize = 2, Cursor = cursor },
                    CancellationToken.None);

                seen.AddRange(page.Value.Items.Select(item => item.ExternalReference));
                cursor = page.Value.NextCursor;
            }
            while (cursor is not null);

            seen.Count.ShouldBe(7);
            seen.ShouldBeUnique();
        }

        [Theory]
        [InlineData(0, null, "pageSize")]
        [InlineData(ListQuotesQuery.MaxPageSize + 1, null, "pageSize")]
        [InlineData(20, "not-a-cursor", "cursor")]
        [InlineData(20, "bm90LWEtY3Vyc29y", "cursor")]
        public async Task ListQuotes_InvalidPaging_ReturnsFieldError(int pageSize, string? cursor, string field)
        {
            Result<PagedResponse<QuoteSummaryResponse>> result = await CreateListHandler().HandleAsync(
                new ListQuotesQuery { PageSize = pageSize, Cursor = cursor },
                CancellationToken.None);

            result.Error!.ValidationErrors!.ShouldContainKey(field);
        }

        [Fact]
        public void QuoteListCursor_RoundTrips()
        {
            QuoteListCursor cursor = new QuoteListCursor { CreatedAt = Now.AddTicks(1234), Id = Guid.CreateVersion7(Now) };

            QuoteListCursor.TryDecode(cursor.Encode(), out QuoteListCursor? decoded).ShouldBeTrue();

            decoded.ShouldBe(cursor);
        }

        [Fact]
        public async Task GetQuote_Existing_ReturnsItWithAllowedNextStatuses()
        {
            Quote quote = TestQuotes.Draft(Now);
            _store.Quotes.Add(quote);
            GetQuoteHandler handler = new GetQuoteHandler(_store);

            Result<QuoteResponse> result = await handler.HandleAsync(new GetQuoteQuery { QuoteId = quote.Id }, CancellationToken.None);

            result.Value.Id.ShouldBe(quote.Id);
            result.Value.AllowedNextStatuses.ShouldBe([QuoteStatus.SentToClient, QuoteStatus.Declined]);
        }

        [Fact]
        public async Task GetQuote_Unknown_ReturnsNotFound()
        {
            GetQuoteHandler handler = new GetQuoteHandler(_store);

            Result<QuoteResponse> result = await handler.HandleAsync(
                new GetQuoteQuery { QuoteId = Guid.CreateVersion7() },
                CancellationToken.None);

            result.Error!.Code.ShouldBe("Quote.NotFound");
        }

        private ListQuotesHandler CreateListHandler()
        {
            return new ListQuotesHandler(new ListQuotesQueryValidator(), _store);
        }
    }
}
