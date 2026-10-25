namespace Quotes.Application.Models.Response.V1
{
    /// <summary>
    /// One page of a keyset-paged list. Pass NextCursor back as the cursor to get the next page; it is null on the last page.
    /// </summary>
    public sealed record PagedResponse<TItem>
    {
        public required IReadOnlyList<TItem> Items { get; init; }

        public required int PageSize { get; init; }

        /// <summary>
        /// How many items match the filters across all pages. Only returned with the first page (no cursor); null on later
        /// pages, so paging deeper never re-counts every match.
        /// </summary>
        public int? TotalCount { get; init; }

        public string? NextCursor { get; init; }
    }
}
