namespace Quotes.Domain.Pricing
{
    /// <summary>
    /// A versioned set of rates. Each quote stores the version it was priced with, so a premium can always be explained later.
    /// </summary>
    public sealed record RateTable
    {
        public required string Version { get; init; }

        public required string Currency { get; init; }

        /// <summary>
        /// The zone whose calendar date decides the client's age (South Africa for ZAR rates). A client whose birthday it
        /// is there is priced at the new age, even while it is still the day before in UTC.
        /// </summary>
        public required TimeZoneInfo TimeZone { get; init; }

        public required decimal SmokerLoading { get; init; }

        public required IReadOnlyList<ProductRate> Products { get; init; }

        public required IReadOnlyList<FactorBand> AgeBands { get; init; }

        public required IReadOnlyList<FactorBand> TermBands { get; init; }

        public ProductRate? FindProduct(string productCode)
        {
            return Products.FirstOrDefault(product =>
                string.Equals(product.ProductCode, productCode, StringComparison.OrdinalIgnoreCase));
        }

        public decimal? AgeFactorFor(int age)
        {
            return AgeBands.FirstOrDefault(band => band.Contains(age))?.Factor;
        }

        public decimal? TermFactorFor(int termYears)
        {
            return TermBands.FirstOrDefault(band => band.Contains(termYears))?.Factor;
        }
    }
}
