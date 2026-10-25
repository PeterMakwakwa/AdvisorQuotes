using Quotes.Domain.Commands;
using Quotes.Domain.Pricing;

namespace Quotes.Application.Models.Request.V1
{
    /// <summary>
    /// Represents a request to capture a new quote. The premium is calculated by the server, never sent by the client.
    /// </summary>
    public sealed record CreateQuoteRequest
    {
        /// <summary>
        /// The adviser capturing the quote.
        /// </summary>
        public required string AdviserId { get; init; }

        /// <summary>
        /// Optional: the adviser's own reference for this quote, for example their CRM case number.
        /// The service issues its own reference (externalReference) either way.
        /// </summary>
        public string? AdviserReference { get; init; }

        /// <summary>
        /// The client being quoted.
        /// </summary>
        public required ClientRequest Client { get; init; }

        /// <summary>
        /// The product and cover being quoted.
        /// </summary>
        public required ProductRequest Product { get; init; }

        /// <summary>
        /// Optional free-text notes from the adviser.
        /// </summary>
        public string? Notes { get; init; }

        /// <summary>
        /// The request in its canonical form, used for duplicate detection: text trimmed, the product code in upper case,
        /// and blank optional fields treated as absent. Two submissions that create the same quote hash the same,
        /// so a retry with harmless differences (spacing, letter case) is replayed instead of rejected.
        /// </summary>
        public CreateQuoteRequest Normalize()
        {
            return this with
            {
                AdviserId = AdviserId.Trim(),
                AdviserReference = NullIfBlank(AdviserReference),
                Client = Client with { FullName = Client.FullName.Trim() },
                Product = Product with { ProductCode = Product.ProductCode.Trim().ToUpperInvariant() },
                Notes = NullIfBlank(Notes)
            };
        }

        public CreateQuote BuildCommand(string externalReference, PremiumCalculation pricing)
        {
            return new CreateQuote
            {
                ExternalReference = externalReference,
                AdviserId = AdviserId,
                AdviserReference = AdviserReference,
                Client = Client.BuildClientDetails(),
                Product = Product.BuildProductDetails(),
                MonthlyPremium = pricing.MonthlyPremium,
                PricingVersion = pricing.PricingVersion,
                Notes = Notes
            };
        }

        private static string? NullIfBlank(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
