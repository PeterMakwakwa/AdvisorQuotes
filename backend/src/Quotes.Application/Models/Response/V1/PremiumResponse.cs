namespace Quotes.Application.Models.Response.V1
{
    public sealed record PremiumResponse
    {
        public required decimal MonthlyAmount { get; init; }

        public required string Currency { get; init; }

        /// <summary>
        /// The rate table version that produced this premium.
        /// </summary>
        public required string PricingVersion { get; init; }
    }
}
