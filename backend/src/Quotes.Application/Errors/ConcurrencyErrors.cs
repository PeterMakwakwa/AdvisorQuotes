using Quotes.Domain.Common;

namespace Quotes.Application.Errors
{
    public static class ConcurrencyErrors
    {
        public static Error QuoteModified(Guid quoteId)
        {
            return Error.Conflict(
                "Quote.ConcurrentUpdate",
                $"Quote '{quoteId}' was changed by another request. Reload it and try again.");
        }
    }
}
