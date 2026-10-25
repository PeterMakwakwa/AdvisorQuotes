using Quotes.Domain.ValueObjects;

namespace Quotes.Domain.Pricing
{
    /// <summary>
    /// The calculated premium plus the factors that produced it, so the result can be explained and traced.
    /// </summary>
    public sealed record PremiumCalculation
    {
        public required Money MonthlyPremium { get; init; }

        public required string PricingVersion { get; init; }

        public required int ClientAge { get; init; }

        public required decimal MonthlyRatePerThousand { get; init; }

        public required decimal AgeFactor { get; init; }

        public required decimal SmokerFactor { get; init; }

        public required decimal TermFactor { get; init; }
    }
}
