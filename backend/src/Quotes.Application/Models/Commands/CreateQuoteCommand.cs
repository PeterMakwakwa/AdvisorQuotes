using Quotes.Application.Models.Request.V1;

namespace Quotes.Application.Models.Commands
{
    public sealed record CreateQuoteCommand
    {
        /// <summary>
        /// The Idempotency-Key header, if the client sent one.
        /// </summary>
        public string? IdempotencyKey { get; init; }

        public required CreateQuoteRequest Request { get; init; }
    }
}
