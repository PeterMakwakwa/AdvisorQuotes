using Microsoft.Extensions.Options;
using Quotes.Domain.Observability.Messages;

namespace Quotes.Infrastructure.Pricing
{
    /// <summary>
    /// Runs at startup (ValidateOnStart): a broken rate table stops the app instead of producing wrong premiums.
    /// </summary>
    internal sealed class PricingOptionsValidator : IValidateOptions<PricingOptions>
    {
        private const int CurrencyCodeLength = 3;

        public ValidateOptionsResult Validate(string? name, PricingOptions options)
        {
            List<string> failures = [];

            if (string.IsNullOrWhiteSpace(options.Version))
            {
                failures.Add(ErrorMessages.PricingVersionRequired);
            }

            if (options.Currency.Length != CurrencyCodeLength)
            {
                failures.Add(MessageFormatter.Format(ErrorMessages.PricingCurrencyInvalid, CurrencyCodeLength));
            }

            if (string.IsNullOrWhiteSpace(options.TimeZone) || !TimeZoneInfo.TryFindSystemTimeZoneById(options.TimeZone, out _))
            {
                failures.Add(MessageFormatter.Format(ErrorMessages.PricingTimeZoneInvalid, options.TimeZone));
            }

            if (options.SmokerLoading < 1)
            {
                failures.Add(ErrorMessages.PricingSmokerLoadingTooLow);
            }

            if (options.Products.Count == 0)
            {
                failures.Add(ErrorMessages.PricingProductsEmpty);
            }

            if (options.Products.GroupBy(product => product.ProductCode, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
            {
                failures.Add(ErrorMessages.PricingProductsDuplicated);
            }

            foreach (ProductRateOptions product in options.Products)
            {
                ValidateProduct(product, failures);
            }

            ValidateBands(nameof(PricingOptions.AgeBands), options.AgeBands, failures);
            ValidateBands(nameof(PricingOptions.TermBands), options.TermBands, failures);

            return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
        }

        private static void ValidateProduct(ProductRateOptions product, List<string> failures)
        {
            if (string.IsNullOrWhiteSpace(product.ProductCode))
            {
                failures.Add(ErrorMessages.PricingProductCodeMissing);
            }

            if (product.MonthlyRatePerThousand <= 0)
            {
                failures.Add(MessageFormatter.Format(ErrorMessages.PricingProductRateNotPositive, product.ProductCode));
            }

            if (product.MinCoverAmount <= 0 || product.MinCoverAmount > product.MaxCoverAmount)
            {
                failures.Add(MessageFormatter.Format(ErrorMessages.PricingProductCoverLimitsInvalid, product.ProductCode));
            }

            if (product.MinTermYears <= 0 || product.MinTermYears > product.MaxTermYears)
            {
                failures.Add(MessageFormatter.Format(ErrorMessages.PricingProductTermLimitsInvalid, product.ProductCode));
            }

            if (product.MaxAgeAtExpiry <= 0)
            {
                failures.Add(MessageFormatter.Format(ErrorMessages.PricingProductMaxAgeAtExpiryNotPositive, product.ProductCode));
            }
        }

        private static void ValidateBands(string sectionName, List<FactorBandOptions> bands, List<string> failures)
        {
            if (bands.Count == 0)
            {
                failures.Add(MessageFormatter.Format(ErrorMessages.PricingBandsEmpty, sectionName));

                return;
            }

            List<FactorBandOptions> ordered = bands.OrderBy(band => band.From).ToList();

            for (int index = 0; index < ordered.Count; index++)
            {
                FactorBandOptions band = ordered[index];

                if (band.From > band.To || band.Factor <= 0)
                {
                    failures.Add(MessageFormatter.Format(ErrorMessages.PricingBandInvalid, sectionName, band.From, band.To));
                }

                if (index > 0 && band.From <= ordered[index - 1].To)
                {
                    failures.Add(MessageFormatter.Format(
                        ErrorMessages.PricingBandsOverlap, sectionName, ordered[index - 1].From, ordered[index - 1].To, band.From, band.To));
                }
            }
        }
    }
}
