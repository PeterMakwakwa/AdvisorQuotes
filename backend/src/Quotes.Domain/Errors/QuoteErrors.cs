using Quotes.Domain.Common;
using Quotes.Domain.Enums;
using Quotes.Domain.Observability.Messages;

namespace Quotes.Domain.Errors
{
    public static class QuoteErrors
    {
        public static readonly Error AdviserRequired =
            Error.Validation(ErrorCodes.QuoteAdviserRequired, ResponseMessages.AdviserRequired);

        public static readonly Error ClientNameRequired =
            Error.Validation(ErrorCodes.QuoteClientNameRequired, ResponseMessages.ClientNameRequired);

        public static readonly Error DateOfBirthNotInPast =
            Error.Validation(ErrorCodes.QuoteDateOfBirthNotInPast, ResponseMessages.ClientDateOfBirthNotInPast);

        public static readonly Error CoverAmountNotPositive =
            Error.Validation(ErrorCodes.QuoteCoverAmountNotPositive, ResponseMessages.CoverAmountNotPositive);

        public static readonly Error TermNotPositive =
            Error.Validation(ErrorCodes.QuoteTermNotPositive, ResponseMessages.TermNotPositive);

        public static readonly Error PremiumNegative =
            Error.Validation(ErrorCodes.QuotePremiumNegative, ResponseMessages.PremiumNegative);

        public static readonly Error ChangedByRequired =
            Error.Validation(ErrorCodes.QuoteChangedByRequired, ResponseMessages.ChangedByRequired);

        public static readonly Error DeclineReasonRequired =
            Error.Validation(ErrorCodes.QuoteDeclineReasonRequired, ResponseMessages.DeclineReasonRequired);

        public static Error StatusUnchanged(QuoteStatus status)
        {
            return Error.Conflict(
                ErrorCodes.QuoteStatusUnchanged,
                MessageFormatter.Format(ResponseMessages.StatusUnchanged, status));
        }

        public static Error InvalidStatusTransition(QuoteStatus from, QuoteStatus to)
        {
            return Error.Conflict(
                ErrorCodes.QuoteInvalidStatusTransition,
                MessageFormatter.Format(ResponseMessages.InvalidStatusTransition, from, to));
        }

        public static Error NotFound(Guid id)
        {
            return Error.NotFound(
                ErrorCodes.QuoteNotFound,
                MessageFormatter.Format(ResponseMessages.QuoteNotFound, id));
        }
    }
}
