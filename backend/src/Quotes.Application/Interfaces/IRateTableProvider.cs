using Quotes.Domain.Pricing;

namespace Quotes.Application.Interfaces
{
    public interface IRateTableProvider
    {
        Task<RateTable> GetCurrentAsync(CancellationToken cancellationToken);
    }
}
