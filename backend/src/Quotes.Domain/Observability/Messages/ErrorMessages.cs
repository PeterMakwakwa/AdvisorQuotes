namespace Quotes.Domain.Observability.Messages
{
    /// <summary>
    /// Exception messages, start-up configuration failures and Error-level log templates. Exception and configuration
    /// messages use string.Format placeholders ({0}); log templates use named placeholders ({EventType}).
    /// </summary>
    public static class ErrorMessages
    {
        public const string FailedResultValue = "Cannot read the value of a failed result.";

        public const string ConcurrencyConflict = "The row was changed by another request.";

        public const string DuplicateIdempotencyKey = "The idempotency key was saved by another request.";

        public const string IntegrationEventNotRegistered = "No integration event type is registered for {0}.";

        public const string ConnectionStringMissing = "Connection string '{0}' is not configured. Set ConnectionStrings__{0}.";

        public const string KnownProxiesInvalid = "ForwardedHeaders:KnownProxies must be IP addresses.";

        public const string KnownNetworksInvalid = "ForwardedHeaders:KnownNetworks must be CIDR ranges such as 10.0.0.0/16.";

        public const string ForwardLimitOutOfRange = "ForwardedHeaders:ForwardLimit must be between {0} and {1}.";

        public const string PricingVersionRequired = "Pricing:Version is required.";

        public const string PricingCurrencyInvalid = "Pricing:Currency must be a {0}-letter ISO code.";

        public const string PricingSmokerLoadingTooLow = "Pricing:SmokerLoading must be at least 1.";

        public const string PricingProductsEmpty = "Pricing:Products must contain at least one product.";

        public const string PricingProductsDuplicated = "Pricing:Products contains duplicate product codes.";

        public const string PricingProductCodeMissing = "Pricing:Products has a product without a ProductCode.";

        public const string PricingProductRateNotPositive = "Pricing:Products[{0}]: MonthlyRatePerThousand must be greater than zero.";

        public const string PricingProductCoverLimitsInvalid = "Pricing:Products[{0}]: cover limits must be positive with Min <= Max.";

        public const string PricingProductTermLimitsInvalid = "Pricing:Products[{0}]: term limits must be positive with Min <= Max.";

        public const string PricingProductMaxAgeAtExpiryNotPositive = "Pricing:Products[{0}]: MaxAgeAtExpiry must be greater than zero.";

        public const string PricingBandsEmpty = "Pricing:{0} must contain at least one band.";

        public const string PricingBandInvalid = "Pricing:{0} band {1}-{2} must have From <= To and a positive Factor.";

        public const string PricingBandsOverlap = "Pricing:{0} bands {1}-{2} and {3}-{4} overlap.";

        public const string ApiTerminatedUnexpectedly = "Advisor Quotes API terminated unexpectedly";

        public const string UnhandledException = "Unhandled exception for {Method} {Path}";

        public const string PublishGaveUp = "Giving up on {EventType} {MessageId} after {Attempts} attempts; it stays in the outbox for investigation";

        public const string OutboxDispatchFailed = "Outbox dispatch failed; retrying on the next tick";

        public const string MaintenanceFailed = "Database maintenance failed; retrying on the next interval";
    }
}
