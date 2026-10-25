using Quotes.Application.Models.Request.V1;

namespace Quotes.Application.Models.Commands
{
    public sealed record ChangeQuoteStatusCommand
    {
        public required Guid QuoteId { get; init; }

        public required ChangeQuoteStatusRequest Request { get; init; }
    }
}
