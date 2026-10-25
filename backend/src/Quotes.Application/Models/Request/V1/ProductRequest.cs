using Quotes.Domain.ValueObjects;

namespace Quotes.Application.Models.Request.V1
{
    /// <summary>
    /// The product and cover being quoted.
    /// </summary>
    public sealed record ProductRequest
    {
        /// <summary>
        /// The product code, for example "TERM_LIFE".
        /// </summary>
        public required string ProductCode { get; init; }

        /// <summary>
        /// The sum assured, in the quote currency.
        /// </summary>
        public required decimal CoverAmount { get; init; }

        /// <summary>
        /// How many years the cover runs for.
        /// </summary>
        public required int TermYears { get; init; }

        public ProductDetails BuildProductDetails()
        {
            return new ProductDetails
            {
                ProductCode = ProductCode.Trim().ToUpperInvariant(),
                CoverAmount = CoverAmount,
                TermYears = TermYears
            };
        }
    }
}
