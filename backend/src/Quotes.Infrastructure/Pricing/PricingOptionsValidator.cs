using Microsoft.Extensions.Options;

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
                failures.Add("Pricing:Version is required.");
            }

            if (options.Currency.Length != CurrencyCodeLength)
            {
                failures.Add("Pricing:Currency must be a 3-letter ISO code.");
            }

            if (options.SmokerLoading < 1)
            {
                failures.Add("Pricing:SmokerLoading must be at least 1.");
            }

            if (options.Products.Count == 0)
            {
                failures.Add("Pricing:Products must contain at least one product.");
            }

            if (options.Products.GroupBy(product => product.ProductCode, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
            {
                failures.Add("Pricing:Products contains duplicate product codes.");
            }

            foreach (ProductRateOptions product in options.Products)
            {
                ValidateProduct(product, failures);
            }

            ValidateBands("AgeBands", options.AgeBands, failures);
            ValidateBands("TermBands", options.TermBands, failures);

            return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
        }

        private static void ValidateProduct(ProductRateOptions product, List<string> failures)
        {
            string label = $"Pricing:Products[{product.ProductCode}]";

            if (string.IsNullOrWhiteSpace(product.ProductCode))
            {
                failures.Add("Pricing:Products has a product without a ProductCode.");
            }

            if (product.MonthlyRatePerThousand <= 0)
            {
                failures.Add($"{label}: MonthlyRatePerThousand must be greater than zero.");
            }

            if (product.MinCoverAmount <= 0 || product.MinCoverAmount > product.MaxCoverAmount)
            {
                failures.Add($"{label}: cover limits must be positive with Min <= Max.");
            }

            if (product.MinTermYears <= 0 || product.MinTermYears > product.MaxTermYears)
            {
                failures.Add($"{label}: term limits must be positive with Min <= Max.");
            }

            if (product.MaxAgeAtExpiry <= 0)
            {
                failures.Add($"{label}: MaxAgeAtExpiry must be greater than zero.");
            }
        }

        private static void ValidateBands(string sectionName, List<FactorBandOptions> bands, List<string> failures)
        {
            if (bands.Count == 0)
            {
                failures.Add($"Pricing:{sectionName} must contain at least one band.");

                return;
            }

            List<FactorBandOptions> ordered = bands.OrderBy(band => band.From).ToList();

            for (int index = 0; index < ordered.Count; index++)
            {
                FactorBandOptions band = ordered[index];

                if (band.From > band.To || band.Factor <= 0)
                {
                    failures.Add($"Pricing:{sectionName} band {band.From}-{band.To} must have From <= To and a positive Factor.");
                }

                if (index > 0 && band.From <= ordered[index - 1].To)
                {
                    failures.Add($"Pricing:{sectionName} bands {ordered[index - 1].From}-{ordered[index - 1].To} and {band.From}-{band.To} overlap.");
                }
            }
        }
    }
}
