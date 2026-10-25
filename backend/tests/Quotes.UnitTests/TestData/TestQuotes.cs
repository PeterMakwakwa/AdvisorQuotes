using Quotes.Domain.Commands;
using Quotes.Domain.Entities;
using Quotes.Domain.ValueObjects;

namespace Quotes.UnitTests.TestData
{
    internal static class TestQuotes
    {
        public static Quote Draft(
            DateTimeOffset createdAt,
            string adviserId = TestRequests.AdviserId,
            string? adviserReference = null,
            string externalReference = "Q-2026-000001")
        {
            return Quote.Create(
                new CreateQuote
                {
                    ExternalReference = externalReference,
                    AdviserId = adviserId,
                    AdviserReference = adviserReference,
                    Client = new ClientDetails { FullName = "Thandi Nkosi", DateOfBirth = new DateOnly(1991, 1, 1), IsSmoker = false },
                    Product = new ProductDetails { ProductCode = TestRates.TermLife, CoverAmount = 1_000_000m, TermYears = 20 },
                    MonthlyPremium = Money.Of(300m, "ZAR"),
                    PricingVersion = "2026.1"
                },
                createdAt).Value;
        }
    }
}
