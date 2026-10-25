using Quotes.Domain.Common;
using Quotes.Domain.Enums;

namespace Quotes.Domain.Errors
{
    public static class QuoteErrors
    {
        public static readonly Error AdviserRequired =
            Error.Validation("Quote.AdviserRequired", "An adviser identifier is required.");

        public static readonly Error ClientNameRequired =
            Error.Validation("Quote.ClientNameRequired", "The client's full name is required.");

        public static readonly Error DateOfBirthNotInPast =
            Error.Validation("Quote.DateOfBirthNotInPast", "The client's date of birth must be in the past.");

        public static readonly Error CoverAmountNotPositive =
            Error.Validation("Quote.CoverAmountNotPositive", "The cover amount must be greater than zero.");

        public static readonly Error TermNotPositive =
            Error.Validation("Quote.TermNotPositive", "The term must be at least one year.");

        public static readonly Error PremiumNegative =
            Error.Validation("Quote.PremiumNegative", "The premium cannot be negative.");

        public static readonly Error ChangedByRequired =
            Error.Validation("Quote.ChangedByRequired", "The person changing the status is required.");

        public static readonly Error DeclineReasonRequired =
            Error.Validation("Quote.DeclineReasonRequired", "A reason is required when a quote is declined.");

        public static Error StatusUnchanged(QuoteStatus status)
        {
            return Error.Conflict("Quote.StatusUnchanged", $"The quote is already {status}.");
        }

        public static Error InvalidStatusTransition(QuoteStatus from, QuoteStatus to)
        {
            return Error.Conflict("Quote.InvalidStatusTransition", $"A quote cannot move from {from} to {to}.");
        }

        public static Error NotFound(Guid id)
        {
            return Error.NotFound("Quote.NotFound", $"Quote '{id}' was not found.");
        }
    }
}
