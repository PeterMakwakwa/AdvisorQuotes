using Quotes.Application.Interfaces;
using Quotes.Domain.Pricing;
using Quotes.UnitTests.TestData;

namespace Quotes.UnitTests.Fakes
{
    internal sealed class FakeRateTableProvider : IRateTableProvider
    {
        public Task<RateTable> GetCurrentAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(TestRates.Table);
        }
    }
}
