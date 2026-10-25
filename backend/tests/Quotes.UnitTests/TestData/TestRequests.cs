using Quotes.Application.Models.Request.V1;

namespace Quotes.UnitTests.TestData
{
    internal static class TestRequests
    {
        public const string AdviserId = "ADV-001";

        public static CreateQuoteRequest CreateQuote(
            decimal coverAmount = 1_000_000m,
            string? notes = null,
            string? adviserReference = null)
        {
            return new CreateQuoteRequest
            {
                AdviserId = AdviserId,
                AdviserReference = adviserReference,
                Client = new ClientRequest
                {
                    FullName = "Thandi Nkosi",
                    DateOfBirth = new DateOnly(1991, 1, 1),
                    IsSmoker = false
                },
                Product = new ProductRequest
                {
                    ProductCode = TestRates.TermLife,
                    CoverAmount = coverAmount,
                    TermYears = 20
                },
                Notes = notes
            };
        }
    }
}
