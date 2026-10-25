using Quotes.Domain.Pricing;

namespace Quotes.UnitTests.TestData
{
    internal static class TestRates
    {
        public const string TermLife = "TERM_LIFE";

        public static readonly RateTable Table = new RateTable
        {
            Version = "2026.1",
            Currency = "ZAR",
            SmokerLoading = 1.50m,
            Products =
            [
                new ProductRate
                {
                    ProductCode = TermLife,
                    MonthlyRatePerThousand = 0.30m,
                    MinCoverAmount = 100_000m,
                    MaxCoverAmount = 10_000_000m,
                    MinTermYears = 5,
                    MaxTermYears = 30,
                    MaxAgeAtExpiry = 75
                }
            ],
            AgeBands =
            [
                new FactorBand { From = 18, To = 30, Factor = 0.80m },
                new FactorBand { From = 31, To = 40, Factor = 1.00m },
                new FactorBand { From = 41, To = 50, Factor = 1.60m },
                new FactorBand { From = 51, To = 60, Factor = 2.60m },
                new FactorBand { From = 61, To = 65, Factor = 4.00m }
            ],
            TermBands =
            [
                new FactorBand { From = 5, To = 10, Factor = 0.95m },
                new FactorBand { From = 11, To = 20, Factor = 1.00m },
                new FactorBand { From = 21, To = 30, Factor = 1.10m }
            ]
        };
    }
}
