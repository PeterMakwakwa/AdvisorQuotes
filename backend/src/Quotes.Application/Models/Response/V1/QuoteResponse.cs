using Quotes.Domain.Entities;
using Quotes.Domain.Enums;
using Quotes.Domain.Rules;

namespace Quotes.Application.Models.Response.V1
{
    /// <summary>
    /// Full quote details, including the status history and which statuses it can move to next.
    /// </summary>
    public sealed record QuoteResponse
    {
        public required Guid Id { get; init; }

        public required string ExternalReference { get; init; }

        public required string AdviserId { get; init; }

        public string? AdviserReference { get; init; }

        public required ClientResponse Client { get; init; }

        public required ProductResponse Product { get; init; }

        public required PremiumResponse Premium { get; init; }

        public required QuoteStatus Status { get; init; }

        /// <summary>
        /// The statuses this quote may move to next, so a UI knows which actions to offer.
        /// </summary>
        public required IReadOnlyList<QuoteStatus> AllowedNextStatuses { get; init; }

        public string? Notes { get; init; }

        public required DateTimeOffset CreatedAt { get; init; }

        public required DateTimeOffset UpdatedAt { get; init; }

        public required IReadOnlyList<StatusChangeResponse> StatusHistory { get; init; }

        public static QuoteResponse FromDomain(Quote quote)
        {
            return new QuoteResponse
            {
                Id = quote.Id,
                ExternalReference = quote.ExternalReference,
                AdviserId = quote.AdviserId,
                AdviserReference = quote.AdviserReference,
                Client = ClientResponse.FromDomain(quote.Client),
                Product = ProductResponse.FromDomain(quote.Product),
                Premium = new PremiumResponse
                {
                    MonthlyAmount = quote.MonthlyPremium.Amount,
                    Currency = quote.MonthlyPremium.Currency,
                    PricingVersion = quote.PricingVersion
                },
                Status = quote.Status,
                AllowedNextStatuses = QuoteStatusTransitions.AllowedFrom(quote.Status),
                Notes = quote.Notes,
                CreatedAt = quote.CreatedAt,
                UpdatedAt = quote.UpdatedAt,
                StatusHistory = quote.StatusHistory
                    .OrderBy(change => change.ChangedAt)
                    .ThenBy(change => change.Id)
                    .Select(StatusChangeResponse.FromDomain)
                    .ToList()
            };
        }
    }
}
