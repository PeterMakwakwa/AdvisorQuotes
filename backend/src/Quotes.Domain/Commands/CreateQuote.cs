using Quotes.Domain.ValueObjects;

namespace Quotes.Domain.Commands
{
    public sealed record CreateQuote
    {
        public required string ExternalReference { get; init; }

        public required string AdviserId { get; init; }

        public string? AdviserReference { get; init; }

        public required ClientDetails Client { get; init; }

        public required ProductDetails Product { get; init; }

        public required Money MonthlyPremium { get; init; }

        public required string PricingVersion { get; init; }

        public string? Notes { get; init; }
    }
}
