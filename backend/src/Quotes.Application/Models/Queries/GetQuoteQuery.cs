namespace Quotes.Application.Models.Queries
{
    public sealed record GetQuoteQuery
    {
        public required Guid QuoteId { get; init; }
    }
}
