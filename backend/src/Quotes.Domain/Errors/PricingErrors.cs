using Quotes.Domain.Common;
using Quotes.Domain.Observability.Messages;

namespace Quotes.Domain.Errors
{
    public static class PricingErrors
    {
        public static Error UnknownProduct(string productCode)
        {
            return Error.Validation(
                ErrorCodes.PricingUnknownProduct,
                MessageFormatter.Format(ResponseMessages.UnknownProduct, productCode));
        }

        public static Error CoverOutOfRange(string productCode, decimal min, decimal max)
        {
            return Error.Validation(
                ErrorCodes.PricingCoverOutOfRange,
                MessageFormatter.Format(ResponseMessages.CoverOutOfRange, productCode, min, max));
        }

        public static Error TermOutOfRange(string productCode, int min, int max)
        {
            return Error.Validation(
                ErrorCodes.PricingTermOutOfRange,
                MessageFormatter.Format(ResponseMessages.TermOutOfRange, productCode, min, max));
        }

        public static Error TermNotRated(int termYears)
        {
            return Error.Validation(
                ErrorCodes.PricingTermNotRated,
                MessageFormatter.Format(ResponseMessages.TermNotRated, termYears));
        }

        public static Error AgeNotInsurable(int age)
        {
            return Error.Validation(
                ErrorCodes.PricingAgeNotInsurable,
                MessageFormatter.Format(ResponseMessages.AgeNotInsurable, age));
        }

        public static Error AgeAtExpiryExceeded(int ageAtExpiry, int maxAgeAtExpiry)
        {
            return Error.Validation(
                ErrorCodes.PricingAgeAtExpiryExceeded,
                MessageFormatter.Format(ResponseMessages.AgeAtExpiryExceeded, ageAtExpiry, maxAgeAtExpiry));
        }
    }
}
