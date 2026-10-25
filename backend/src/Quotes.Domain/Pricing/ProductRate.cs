namespace Quotes.Domain.Pricing
{
    /// <summary>
    /// The base rate and underwriting limits for one product.
    /// </summary>
    public sealed record ProductRate
    {
        public required string ProductCode { get; init; }

        public required decimal MonthlyRatePerThousand { get; init; }

        public required decimal MinCoverAmount { get; init; }

        public required decimal MaxCoverAmount { get; init; }

        public required int MinTermYears { get; init; }

        public required int MaxTermYears { get; init; }

        public required int MaxAgeAtExpiry { get; init; }
    }
}
