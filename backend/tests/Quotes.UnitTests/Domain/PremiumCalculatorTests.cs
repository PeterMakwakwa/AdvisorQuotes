using Quotes.Domain.Common;
using Quotes.Domain.Pricing;
using Quotes.Domain.ValueObjects;
using Quotes.UnitTests.TestData;
using Shouldly;

namespace Quotes.UnitTests.Domain
{
    public class PremiumCalculatorTests
    {
        private const string TermLife = TestRates.TermLife;

        private static readonly DateOnly AsOf = new DateOnly(2026, 10, 24);

        private static readonly RateTable Rates = TestRates.Table;

        [Fact]
        public void Calculate_BaseCase_AppliesProductRateOnly()
        {
            // 1 000 units x 0.30 x age 1.00 x non-smoker 1.00 x term 1.00
            Result<PremiumCalculation> result = Calculate(Client(age: 35), Product(1_000_000m, 20));

            result.IsSuccess.ShouldBeTrue();
            result.Value.MonthlyPremium.ShouldBe(Money.Of(300.00m, "ZAR"));
            result.Value.PricingVersion.ShouldBe("2026.1");
            result.Value.ClientAge.ShouldBe(35);
        }

        [Fact]
        public void Calculate_Smoker_AppliesSmokerLoading()
        {
            Result<PremiumCalculation> result = Calculate(Client(age: 35, isSmoker: true), Product(1_000_000m, 20));

            result.Value.MonthlyPremium.Amount.ShouldBe(450.00m);
            result.Value.SmokerFactor.ShouldBe(1.50m);
        }

        [Fact]
        public void Calculate_OlderClient_AppliesAgeBandFactor()
        {
            Result<PremiumCalculation> result = Calculate(Client(age: 45), Product(1_000_000m, 20));

            result.Value.MonthlyPremium.Amount.ShouldBe(480.00m);
            result.Value.AgeFactor.ShouldBe(1.60m);
        }

        [Fact]
        public void Calculate_LongTerm_AppliesTermBandFactor()
        {
            Result<PremiumCalculation> result = Calculate(Client(age: 35), Product(1_000_000m, 25));

            result.Value.MonthlyPremium.Amount.ShouldBe(330.00m);
            result.Value.TermFactor.ShouldBe(1.10m);
        }

        [Fact]
        public void Calculate_RoundsOnceAtTheEnd()
        {
            // 333.333 units x 0.30 = 99.9999, which rounds to 100.00
            Result<PremiumCalculation> result = Calculate(Client(age: 35), Product(333_333m, 20));

            result.Value.MonthlyPremium.Amount.ShouldBe(100.00m);
        }

        [Fact]
        public void Calculate_UnknownProduct_Fails()
        {
            ProductDetails product = Product(1_000_000m, 20) with { ProductCode = "PET_INSURANCE" };

            Result<PremiumCalculation> result = Calculate(Client(age: 35), product);

            result.Error!.Code.ShouldBe("Pricing.UnknownProduct");
        }

        [Theory]
        [InlineData(50_000)]
        [InlineData(20_000_000)]
        public void Calculate_CoverOutsideProductLimits_Fails(int coverAmount)
        {
            Result<PremiumCalculation> result = Calculate(Client(age: 35), Product(coverAmount, 20));

            result.Error!.Code.ShouldBe("Pricing.CoverOutOfRange");
        }

        [Fact]
        public void Calculate_TermOutsideProductLimits_Fails()
        {
            Result<PremiumCalculation> result = Calculate(Client(age: 35), Product(1_000_000m, 40));

            result.Error!.Code.ShouldBe("Pricing.TermOutOfRange");
        }

        [Fact]
        public void Calculate_AgeWithoutBand_Fails()
        {
            Result<PremiumCalculation> result = Calculate(Client(age: 70), Product(1_000_000m, 5));

            result.Error!.Code.ShouldBe("Pricing.AgeNotInsurable");
        }

        [Fact]
        public void Calculate_CoverEndingAfterMaxAge_Fails()
        {
            // Aged 60 with a 20-year term ends at 80, above the product maximum of 75
            Result<PremiumCalculation> result = Calculate(Client(age: 60), Product(1_000_000m, 20));

            result.Error!.Code.ShouldBe("Pricing.AgeAtExpiryExceeded");
        }

        [Fact]
        public void Calculate_ProductCodeIsCaseInsensitive()
        {
            ProductDetails product = Product(1_000_000m, 20) with { ProductCode = "term_life" };

            Result<PremiumCalculation> result = Calculate(Client(age: 35), product);

            result.IsSuccess.ShouldBeTrue();
        }

        private static Result<PremiumCalculation> Calculate(ClientDetails client, ProductDetails product)
        {
            return PremiumCalculator.Calculate(client, product, Rates, AsOf);
        }

        private static ClientDetails Client(int age, bool isSmoker = false)
        {
            return new ClientDetails
            {
                FullName = "Thandi Nkosi",
                DateOfBirth = AsOf.AddYears(-age),
                IsSmoker = isSmoker
            };
        }

        private static ProductDetails Product(decimal coverAmount, int termYears)
        {
            return new ProductDetails
            {
                ProductCode = TermLife,
                CoverAmount = coverAmount,
                TermYears = termYears
            };
        }
    }
}
