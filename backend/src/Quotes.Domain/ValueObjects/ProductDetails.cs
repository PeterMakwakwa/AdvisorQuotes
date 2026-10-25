namespace Quotes.Domain.ValueObjects
{
    public sealed record ProductDetails
    {
        public required string ProductCode { get; init; }

        public required decimal CoverAmount { get; init; }

        public required int TermYears { get; init; }
    }
}
