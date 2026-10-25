using Quotes.Application.Models.Queries;
using Quotes.Domain.Enums;

namespace Quotes.Application.Models.Request.V1
{
    /// <summary>
    /// Query-string filters and paging for listing quotes, newest first. Bound from the URL, then turned into a ListQuotesQuery.
    /// </summary>
    public sealed record ListQuotesRequest
    {
        /// <summary>
        /// Only return quotes captured by this adviser.
        /// </summary>
        public string? AdviserId { get; init; }

        /// <summary>
        /// Only return quotes with this adviser reference (exact match), for example to find every quote for one CRM case.
        /// </summary>
        public string? AdviserReference { get; init; }

        /// <summary>
        /// Only return quotes in this status.
        /// </summary>
        public QuoteStatus? Status { get; init; }

        /// <summary>
        /// Only return quotes for a client with this full name (ignoring case and surrounding spaces).
        /// </summary>
        public string? ClientName { get; init; }

        /// <summary>
        /// Only return quotes for a client born on this date (yyyy-MM-dd).
        /// </summary>
        public DateOnly? ClientDateOfBirth { get; init; }

        /// <summary>
        /// Only return quotes for this product, for example TERM_LIFE (ignoring case).
        /// </summary>
        public string? ProductCode { get; init; }

        /// <summary>
        /// Only return open quotes: those that can still change status (Draft or SentToClient). Used to warn an adviser
        /// before capturing another quote for a client who already has one.
        /// </summary>
        public bool OpenOnly { get; init; }

        /// <summary>
        /// The nextCursor from the previous page; omit for the first (newest) page.
        /// </summary>
        public string? Cursor { get; init; }

        /// <summary>
        /// How many quotes per page (1 to 100).
        /// </summary>
        public int PageSize { get; init; } = ListQuotesQuery.DefaultPageSize;

        public ListQuotesQuery BuildQuery()
        {
            return new ListQuotesQuery
            {
                AdviserId = AdviserId,
                AdviserReference = AdviserReference,
                Status = Status,
                ClientName = ClientName,
                ClientDateOfBirth = ClientDateOfBirth,
                ProductCode = ProductCode,
                OpenOnly = OpenOnly,
                Cursor = Cursor,
                PageSize = PageSize
            };
        }
    }
}
