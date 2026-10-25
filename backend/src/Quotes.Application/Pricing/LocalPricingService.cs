using System.Diagnostics;
using Quotes.Application.Interfaces;
using Quotes.Application.Observability;
using Quotes.Domain.Common;
using Quotes.Domain.Pricing;
using Quotes.Domain.ValueObjects;

namespace Quotes.Application.Pricing
{
    /// <summary>
    /// In-process pricing: loads the current rate table and runs the domain PremiumCalculator.
    /// </summary>
    public sealed class LocalPricingService : IPricingService
    {
        private readonly IRateTableProvider _rateTableProvider;

        private readonly TimeProvider _timeProvider;

        public LocalPricingService(IRateTableProvider rateTableProvider, TimeProvider timeProvider)
        {
            _rateTableProvider = rateTableProvider;
            _timeProvider = timeProvider;
        }

        public async Task<Result<PremiumCalculation>> PriceAsync(
            ClientDetails client,
            ProductDetails product,
            CancellationToken cancellationToken)
        {
            using Activity? activity = QuotesTelemetry.ActivitySource.StartActivity(QuotesTelemetry.SpanNames.CalculatePremium);
            activity?.SetTag(QuotesTelemetry.TagNames.ProductCode, product.ProductCode);

            RateTable rates = await _rateTableProvider.GetCurrentAsync(cancellationToken);
            DateOnly asOf = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(_timeProvider.GetUtcNow(), rates.TimeZone).DateTime);

            Result<PremiumCalculation> result = PremiumCalculator.Calculate(client, product, rates, asOf);

            if (result.IsFailure)
            {
                QuotesTelemetry.RecordFailure(activity, result.Error!);

                return result;
            }

            PremiumCalculation calculation = result.Value;
            activity?.SetTag(QuotesTelemetry.TagNames.PricingVersion, calculation.PricingVersion);
            activity?.SetTag(QuotesTelemetry.TagNames.ClientAge, calculation.ClientAge);
            activity?.SetTag(QuotesTelemetry.TagNames.AgeFactor, calculation.AgeFactor);
            activity?.SetTag(QuotesTelemetry.TagNames.SmokerFactor, calculation.SmokerFactor);
            activity?.SetTag(QuotesTelemetry.TagNames.TermFactor, calculation.TermFactor);
            activity?.SetTag(QuotesTelemetry.TagNames.MonthlyPremium, calculation.MonthlyPremium.Amount);

            return result;
        }
    }
}
