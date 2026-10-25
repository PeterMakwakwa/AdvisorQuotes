using Quotes.Domain.ValueObjects;

namespace Quotes.Application.Models.Response.V1
{
    public sealed record ProductResponse
    {
        public required string ProductCode { get; init; }

        public required decimal CoverAmount { get; init; }

        public required int TermYears { get; init; }

        public static ProductResponse FromDomain(ProductDetails product)
        {
            return new ProductResponse
            {
                ProductCode = product.ProductCode,
                CoverAmount = product.CoverAmount,
                TermYears = product.TermYears
            };
        }
    }
}
