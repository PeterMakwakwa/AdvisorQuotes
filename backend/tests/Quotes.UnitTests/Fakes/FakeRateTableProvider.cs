using Quotes.Application.Interfaces;
using Quotes.Domain.Pricing;
using Quotes.UnitTests.TestData;

namespace Quotes.UnitTests.Fakes
{
    internal sealed class FakeRateTableProvider : IRateTableProvider
    {
        private readonly RateTable _rates;

        public FakeRateTableProvider(RateTable? rates = null)
        {
            _rates = rates ?? TestRates.Table;
        }

        public Task<RateTable> GetCurrentAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_rates);
        }
    }
}
