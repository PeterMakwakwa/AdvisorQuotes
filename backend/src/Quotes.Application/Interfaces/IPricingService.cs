using Quotes.Domain.Common;
using Quotes.Domain.Pricing;
using Quotes.Domain.ValueObjects;

namespace Quotes.Application.Interfaces
{
    /// <summary>
    /// The seam between quoting and pricing. Today it runs in-process (LocalPricingService);
    /// if pricing becomes its own service, an HTTP/gRPC client implements this interface instead.
    /// </summary>
    public interface IPricingService
    {
        Task<Result<PremiumCalculation>> PriceAsync(
            ClientDetails client,
            ProductDetails product,
            CancellationToken cancellationToken);
    }
}
