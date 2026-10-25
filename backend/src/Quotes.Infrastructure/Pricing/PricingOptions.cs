using Quotes.Domain.Pricing;

namespace Quotes.Infrastructure.Pricing
{
    /// <summary>
    /// The "Pricing" configuration section. Rates are data, so they can change without a code change.
    /// </summary>
    public sealed class PricingOptions
    {
        public const string SectionName = "Pricing";

        public string Version { get; set; } = string.Empty;

        public string Currency { get; set; } = string.Empty;

        public decimal SmokerLoading { get; set; }

        public List<ProductRateOptions> Products { get; set; } = [];

        public List<FactorBandOptions> AgeBands { get; set; } = [];

        public List<FactorBandOptions> TermBands { get; set; } = [];

        public RateTable ToRateTable()
        {
            return new RateTable
            {
                Version = Version,
                Currency = Currency,
                SmokerLoading = SmokerLoading,
                Products = Products.Select(product => product.ToProductRate()).ToList(),
                AgeBands = AgeBands.Select(band => band.ToFactorBand()).ToList(),
                TermBands = TermBands.Select(band => band.ToFactorBand()).ToList()
            };
        }
    }

    public sealed class ProductRateOptions
    {
        public string ProductCode { get; set; } = string.Empty;

        public decimal MonthlyRatePerThousand { get; set; }

        public decimal MinCoverAmount { get; set; }

        public decimal MaxCoverAmount { get; set; }

        public int MinTermYears { get; set; }

        public int MaxTermYears { get; set; }

        public int MaxAgeAtExpiry { get; set; }

        public ProductRate ToProductRate()
        {
            return new ProductRate
            {
                ProductCode = ProductCode.ToUpperInvariant(),
                MonthlyRatePerThousand = MonthlyRatePerThousand,
                MinCoverAmount = MinCoverAmount,
                MaxCoverAmount = MaxCoverAmount,
                MinTermYears = MinTermYears,
                MaxTermYears = MaxTermYears,
                MaxAgeAtExpiry = MaxAgeAtExpiry
            };
        }
    }

    public sealed class FactorBandOptions
    {
        public int From { get; set; }

        public int To { get; set; }

        public decimal Factor { get; set; }

        public FactorBand ToFactorBand()
        {
            return new FactorBand { From = From, To = To, Factor = Factor };
        }
    }
}
