using Quotes.Domain.Common;
using Quotes.Domain.Errors;
using Quotes.Domain.Observability.Messages;

namespace Quotes.Application.Errors
{
    public static class ConcurrencyErrors
    {
        public static Error QuoteModified(Guid quoteId)
        {
            return Error.Conflict(
                ErrorCodes.QuoteConcurrentUpdate,
                MessageFormatter.Format(ResponseMessages.QuoteConcurrentUpdate, quoteId));
        }
    }
}
