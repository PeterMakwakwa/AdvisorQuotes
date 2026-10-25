using Quotes.Domain.Enums;

namespace Quotes.Domain.Commands
{
    public sealed record ChangeQuoteStatus
    {
        public required QuoteStatus NewStatus { get; init; }

        public string? Reason { get; init; }

        public required string ChangedBy { get; init; }
    }
}
