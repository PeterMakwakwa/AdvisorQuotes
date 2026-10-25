using Quotes.Domain.Entities;
using Quotes.Domain.Enums;

namespace Quotes.Application.Models.Response.V1
{
    /// <summary>
    /// One row in a quote list: enough to show and pick a quote, without the history.
    /// </summary>
    public sealed record QuoteSummaryResponse
    {
        public required Guid Id { get; init; }

        public required string ExternalReference { get; init; }

        public required string AdviserId { get; init; }

        public string? AdviserReference { get; init; }

        public required string ClientName { get; init; }

        public required string ProductCode { get; init; }

        public required decimal CoverAmount { get; init; }

        public required decimal MonthlyPremium { get; init; }

        public required string Currency { get; init; }

        public required QuoteStatus Status { get; init; }

        public required DateTimeOffset CreatedAt { get; init; }

        public required DateTimeOffset UpdatedAt { get; init; }

        public static QuoteSummaryResponse FromDomain(Quote quote)
        {
            return new QuoteSummaryResponse
            {
                Id = quote.Id,
                ExternalReference = quote.ExternalReference,
                AdviserId = quote.AdviserId,
                AdviserReference = quote.AdviserReference,
                ClientName = quote.Client.FullName,
                ProductCode = quote.Product.ProductCode,
                CoverAmount = quote.Product.CoverAmount,
                MonthlyPremium = quote.MonthlyPremium.Amount,
                Currency = quote.MonthlyPremium.Currency,
                Status = quote.Status,
                CreatedAt = quote.CreatedAt,
                UpdatedAt = quote.UpdatedAt
            };
        }
    }
}
