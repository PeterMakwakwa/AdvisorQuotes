using Quotes.Domain.Common;
using Quotes.Domain.Errors;
using Quotes.Domain.ValueObjects;

namespace Quotes.Domain.Pricing
{
    /// <summary>
    /// Calculates a monthly premium:
    /// (cover / 1000) x product rate per thousand x age factor x smoker factor x term factor,
    /// rounded once, at the end, to 2 decimal places.
    /// </summary>
    public static class PremiumCalculator
    {
        private const decimal CoverUnit = 1000m;

        private const decimal NonSmokerFactor = 1.00m;

        public static Result<PremiumCalculation> Calculate(
            ClientDetails client,
            ProductDetails product,
            RateTable rates,
            DateOnly asOf)
        {
            ProductRate? productRate = rates.FindProduct(product.ProductCode);

            if (productRate is null)
            {
                return PricingErrors.UnknownProduct(product.ProductCode);
            }

            if (product.CoverAmount < productRate.MinCoverAmount || product.CoverAmount > productRate.MaxCoverAmount)
            {
                return PricingErrors.CoverOutOfRange(
                    productRate.ProductCode, productRate.MinCoverAmount, productRate.MaxCoverAmount);
            }

            if (product.TermYears < productRate.MinTermYears || product.TermYears > productRate.MaxTermYears)
            {
                return PricingErrors.TermOutOfRange(
                    productRate.ProductCode, productRate.MinTermYears, productRate.MaxTermYears);
            }

            int age = client.AgeOn(asOf);
            decimal? ageFactor = rates.AgeFactorFor(age);

            if (ageFactor is null)
            {
                return PricingErrors.AgeNotInsurable(age);
            }

            int ageAtExpiry = age + product.TermYears;

            if (ageAtExpiry > productRate.MaxAgeAtExpiry)
            {
                return PricingErrors.AgeAtExpiryExceeded(ageAtExpiry, productRate.MaxAgeAtExpiry);
            }

            decimal? termFactor = rates.TermFactorFor(product.TermYears);

            if (termFactor is null)
            {
                return PricingErrors.TermNotRated(product.TermYears);
            }

            decimal smokerFactor = client.IsSmoker ? rates.SmokerLoading : NonSmokerFactor;

            decimal monthlyPremium = (product.CoverAmount / CoverUnit)
                * productRate.MonthlyRatePerThousand
                * ageFactor.Value
                * smokerFactor
                * termFactor.Value;

            return new PremiumCalculation
            {
                MonthlyPremium = Money.Of(monthlyPremium, rates.Currency),
                PricingVersion = rates.Version,
                ClientAge = age,
                MonthlyRatePerThousand = productRate.MonthlyRatePerThousand,
                AgeFactor = ageFactor.Value,
                SmokerFactor = smokerFactor,
                TermFactor = termFactor.Value
            };
        }
    }
}
