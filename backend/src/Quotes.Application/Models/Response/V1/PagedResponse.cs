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
        /// How many items match the filters across all pages.
        /// </summary>
        public required int TotalCount { get; init; }

        public string? NextCursor { get; init; }
    }
}
