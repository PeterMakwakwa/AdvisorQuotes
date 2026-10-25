using Microsoft.Extensions.Time.Testing;
using Quotes.Application.Pricing;
using Quotes.Domain.Common;
using Quotes.Domain.Pricing;
using Quotes.Domain.ValueObjects;
using Quotes.UnitTests.Fakes;
using Quotes.UnitTests.TestData;
using Shouldly;

namespace Quotes.UnitTests.Application
{
    public class LocalPricingServiceTests
    {
        private static readonly ClientDetails BornOn15March1985 = new ClientDetails
        {
            FullName = "Thandi Nkosi",
            DateOfBirth = new DateOnly(1985, 3, 15),
            IsSmoker = false
        };

        private static readonly ProductDetails TermLife = new ProductDetails
        {
            ProductCode = TestRates.TermLife,
            CoverAmount = 1_000_000m,
            TermYears = 20
        };

        // 22:30 UTC on 14 March is 00:30 on 15 March in Johannesburg (UTC+2): already the client's 41st birthday there.
        private static readonly DateTimeOffset LateEveningUtc = new DateTimeOffset(2026, 3, 14, 22, 30, 0, TimeSpan.Zero);

        [Fact]
        public async Task Age_IsWorkedOutOnTheCalendarDateInThePricingTimeZone()
        {
            RateTable johannesburg = TestRates.Table with { TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Africa/Johannesburg") };

            PremiumCalculation calculation = await PriceAsync(johannesburg);

            calculation.ClientAge.ShouldBe(41);
        }

        [Fact]
        public async Task Age_InUtc_IsStillTheDayBefore()
        {
            PremiumCalculation calculation = await PriceAsync(TestRates.Table);

            calculation.ClientAge.ShouldBe(40);
        }

        private static async Task<PremiumCalculation> PriceAsync(RateTable rates)
        {
            LocalPricingService service = new LocalPricingService(new FakeRateTableProvider(rates), new FakeTimeProvider(LateEveningUtc));

            Result<PremiumCalculation> result = await service.PriceAsync(BornOn15March1985, TermLife, CancellationToken.None);

            return result.Value;
        }
    }
}
