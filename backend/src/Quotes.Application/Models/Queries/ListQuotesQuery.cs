using Quotes.Domain.Enums;

namespace Quotes.Application.Models.Queries
{
    /// <summary>
    /// Read-only query: quotes newest first, optionally filtered, one page at a time (keyset paging).
    /// </summary>
    public sealed record ListQuotesQuery
    {
        public const int DefaultPageSize = 20;

        public const int MaxPageSize = 100;

        public string? AdviserId { get; init; }

        public string? AdviserReference { get; init; }

        public QuoteStatus? Status { get; init; }

        /// <summary>
        /// The client's full name, ignoring case and surrounding spaces.
        /// </summary>
        public string? ClientName { get; init; }

        public DateOnly? ClientDateOfBirth { get; init; }

        public string? ProductCode { get; init; }

        /// <summary>
        /// Only quotes that can still change status (not yet accepted or declined).
        /// </summary>
        public bool OpenOnly { get; init; }

        /// <summary>
        /// The nextCursor of the previous page; null for the first (newest) page.
        /// </summary>
        public string? Cursor { get; init; }

        public int PageSize { get; init; } = DefaultPageSize;
    }
}
