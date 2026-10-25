using Quotes.Application.Idempotency;
using Quotes.Application.Models.Request.V1;
using Quotes.UnitTests.TestData;
using Shouldly;

namespace Quotes.UnitTests.Application
{
    public class RequestNormalizationTests
    {
        [Fact]
        public void Normalize_TrimsTextUpperCasesProductAndDropsBlankOptionals()
        {
            CreateQuoteRequest request = TestRequests.CreateQuote(notes: "  ", adviserReference: " CRM-1 ") with
            {
                AdviserId = " ADV-001 ",
                Client = TestRequests.CreateQuote().Client with { FullName = "  Thandi Nkosi " },
                Product = TestRequests.CreateQuote().Product with { ProductCode = " term_life" }
            };

            CreateQuoteRequest normalized = request.Normalize();

            normalized.AdviserId.ShouldBe("ADV-001");
            normalized.AdviserReference.ShouldBe("CRM-1");
            normalized.Client.FullName.ShouldBe("Thandi Nkosi");
            normalized.Product.ProductCode.ShouldBe("TERM_LIFE");
            normalized.Notes.ShouldBeNull();
        }

        [Fact]
        public void Hash_IsTheSameForEquivalentRequests()
        {
            CreateQuoteRequest original = TestRequests.CreateQuote();
            CreateQuoteRequest equivalent = original with
            {
                Product = original.Product with { ProductCode = "term_life " },
                Notes = " "
            };

            RequestHasher.Compute(equivalent.Normalize()).ShouldBe(RequestHasher.Compute(original.Normalize()));
        }

        [Fact]
        public void Hash_DiffersWhenTheQuoteWouldDiffer()
        {
            CreateQuoteRequest original = TestRequests.CreateQuote();
            CreateQuoteRequest different = original with
            {
                Product = original.Product with { CoverAmount = original.Product.CoverAmount + 1 }
            };

            RequestHasher.Compute(different.Normalize()).ShouldNotBe(RequestHasher.Compute(original.Normalize()));
        }
    }
}
