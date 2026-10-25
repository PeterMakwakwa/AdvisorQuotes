namespace Quotes.Domain.Observability.Messages
{
    /// <summary>
    /// What happened to a quote: log templates for its lifecycle (named placeholders) and the reasons kept in its status history.
    /// </summary>
    public static class AuditMessages
    {
        public const string QuoteCreatedReason = "Quote created";

        public const string QuoteCreated =
            "Quote {QuoteId} ({ExternalReference}) created for adviser {AdviserId}: {Premium} {Currency} per month, pricing {PricingVersion}";

        public const string QuoteReplayed = "Duplicate submission for adviser {AdviserId} replayed quote {QuoteId} (detected by {KeySource})";

        public const string QuoteStatusChanged = "Quote {QuoteId} moved from {FromStatus} to {ToStatus} by {ChangedBy}";
    }
}
