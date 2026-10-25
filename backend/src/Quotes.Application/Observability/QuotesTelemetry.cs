using System.Diagnostics;
using Quotes.Domain.Common;

namespace Quotes.Application.Observability
{
    /// <summary>
    /// The application's own trace spans. The host registers <see cref="ActivitySourceName"/> with OpenTelemetry, so these
    /// spans appear inside the HTTP request trace, next to the database spans.
    /// </summary>
    public static class QuotesTelemetry
    {
        public const string ActivitySourceName = "Quotes.Application";

        public static readonly ActivitySource ActivitySource = new ActivitySource(ActivitySourceName);

        public static void RecordFailure(Activity? activity, Error error)
        {
            if (activity is null)
            {
                return;
            }

            activity.SetTag(TagNames.ErrorCode, error.Code);
            activity.SetStatus(ActivityStatusCode.Error, error.Code);
        }

        public static class SpanNames
        {
            public const string CreateQuote = "quote.create";

            public const string ChangeQuoteStatus = "quote.status.change";

            public const string CalculatePremium = "pricing.calculate";
        }

        public static class TagNames
        {
            public const string QuoteId = "quote.id";

            public const string QuoteReference = "quote.reference";

            public const string AdviserId = "adviser.id";

            public const string StatusFrom = "quote.status.from";

            public const string StatusTo = "quote.status.to";

            public const string ChangedBy = "quote.status.changed_by";

            public const string IdempotencyKeySource = "idempotency.key_source";

            public const string IdempotencyReplayed = "idempotency.replayed";

            public const string ProductCode = "pricing.product_code";

            public const string ClientAge = "pricing.client_age";

            public const string AgeFactor = "pricing.age_factor";

            public const string SmokerFactor = "pricing.smoker_factor";

            public const string TermFactor = "pricing.term_factor";

            public const string MonthlyPremium = "pricing.monthly_premium";

            public const string PricingVersion = "pricing.version";

            public const string Currency = "quote.currency";

            public const string Operation = "quote.operation";

            public const string ErrorCode = "error.code";

            public const string ErrorType = "error.type";
        }
    }
}
