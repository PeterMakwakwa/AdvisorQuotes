using System.Globalization;
using Quotes.Domain.Common;

namespace Quotes.Domain.Errors
{
    public static class PricingErrors
    {
        public static Error UnknownProduct(string productCode)
        {
            return Error.Validation("Pricing.UnknownProduct", $"Product '{productCode}' is not available for quoting.");
        }

        public static Error CoverOutOfRange(string productCode, decimal min, decimal max)
        {
            return Error.Validation(
                "Pricing.CoverOutOfRange",
                string.Create(CultureInfo.InvariantCulture, $"Cover for {productCode} must be between {min:N0} and {max:N0}."));
        }

        public static Error TermOutOfRange(string productCode, int min, int max)
        {
            return Error.Validation(
                "Pricing.TermOutOfRange",
                $"The term for {productCode} must be between {min} and {max} years.");
        }

        public static Error TermNotRated(int termYears)
        {
            return Error.Validation("Pricing.TermNotRated", $"No rate is configured for a {termYears}-year term.");
        }

        public static Error AgeNotInsurable(int age)
        {
            return Error.Validation("Pricing.AgeNotInsurable", $"A client aged {age} cannot be quoted.");
        }

        public static Error AgeAtExpiryExceeded(int ageAtExpiry, int maxAgeAtExpiry)
        {
            return Error.Validation(
                "Pricing.AgeAtExpiryExceeded",
                $"The client would be {ageAtExpiry} when cover ends; the maximum is {maxAgeAtExpiry}.");
        }
    }
}
