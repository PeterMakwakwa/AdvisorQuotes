namespace Quotes.Domain.Errors
{
    /// <summary>
    /// Stable error codes returned as the "code" extension of a problem response. Clients act on these, so they are part
    /// of the API contract and must not change.
    /// </summary>
    public static class ErrorCodes
    {
        public const string ValidationFailed = "Validation.Failed";

        public const string RateLimitExceeded = "RateLimit.Exceeded";

        public const string QuoteAdviserRequired = "Quote.AdviserRequired";

        public const string QuoteClientNameRequired = "Quote.ClientNameRequired";

        public const string QuoteDateOfBirthNotInPast = "Quote.DateOfBirthNotInPast";

        public const string QuoteCoverAmountNotPositive = "Quote.CoverAmountNotPositive";

        public const string QuoteTermNotPositive = "Quote.TermNotPositive";

        public const string QuotePremiumNegative = "Quote.PremiumNegative";

        public const string QuoteChangedByRequired = "Quote.ChangedByRequired";

        public const string QuoteDeclineReasonRequired = "Quote.DeclineReasonRequired";

        public const string QuoteStatusUnchanged = "Quote.StatusUnchanged";

        public const string QuoteInvalidStatusTransition = "Quote.InvalidStatusTransition";

        public const string QuoteNotFound = "Quote.NotFound";

        public const string QuoteConcurrentUpdate = "Quote.ConcurrentUpdate";

        public const string PricingUnknownProduct = "Pricing.UnknownProduct";

        public const string PricingCoverOutOfRange = "Pricing.CoverOutOfRange";

        public const string PricingTermOutOfRange = "Pricing.TermOutOfRange";

        public const string PricingTermNotRated = "Pricing.TermNotRated";

        public const string PricingAgeNotInsurable = "Pricing.AgeNotInsurable";

        public const string PricingAgeAtExpiryExceeded = "Pricing.AgeAtExpiryExceeded";

        public const string IdempotencyKeyTooLong = "Idempotency.KeyTooLong";

        public const string IdempotencyKeyReused = "Idempotency.KeyReused";
    }
}
