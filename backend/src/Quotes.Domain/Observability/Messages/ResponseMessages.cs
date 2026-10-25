namespace Quotes.Domain.Observability.Messages
{
    /// <summary>
    /// Text returned to API clients in problem responses. Placeholders ({0}, {1}) are filled with string.Format.
    /// </summary>
    public static class ResponseMessages
    {
        public const string ValidationFailed = "One or more validation errors occurred.";

        public const string UnexpectedError = "An unexpected error occurred.";

        public const string TooManyRequestsTitle = "Too Many Requests";

        public const string TooManyRequests = "Too many requests. Try again in {0} seconds.";

        public const string AdviserRequired = "An adviser identifier is required.";

        public const string ClientNameRequired = "The client's full name is required.";

        public const string ClientDateOfBirthNotInPast = "The client's date of birth must be in the past.";

        public const string CoverAmountNotPositive = "The cover amount must be greater than zero.";

        public const string TermNotPositive = "The term must be at least one year.";

        public const string PremiumNegative = "The premium cannot be negative.";

        public const string ChangedByRequired = "The person changing the status is required.";

        public const string DeclineReasonRequired = "A reason is required when a quote is declined.";

        public const string StatusUnchanged = "The quote is already {0}.";

        public const string InvalidStatusTransition = "A quote cannot move from {0} to {1}.";

        public const string QuoteNotFound = "Quote '{0}' was not found.";

        public const string QuoteConcurrentUpdate = "Quote '{0}' was changed by another request. Reload it and try again.";

        public const string UnknownProduct = "Product '{0}' is not available for quoting.";

        public const string CoverOutOfRange = "Cover for {0} must be between {1:N0} and {2:N0}.";

        public const string TermOutOfRange = "The term for {0} must be between {1} and {2} years.";

        public const string TermNotRated = "No rate is configured for a {0}-year term.";

        public const string AgeNotInsurable = "A client aged {0} cannot be quoted.";

        public const string AgeAtExpiryExceeded = "The client would be {0} when cover ends; the maximum is {1}.";

        public const string IdempotencyKeyTooLong = "The Idempotency-Key header must be at most {0} characters.";

        public const string IdempotencyKeyReused = "This Idempotency-Key was already used with a different request body.";

        public const string DateOfBirthNotInPast = "Date of birth must be in the past.";

        public const string DateOfBirthInvalid = "Date of birth is not valid.";

        public const string CursorInvalid = "The cursor is not valid. Use the nextCursor value from the previous page.";
    }
}
