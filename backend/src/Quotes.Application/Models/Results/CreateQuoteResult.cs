using Quotes.Application.Idempotency;
using Quotes.Application.Models.Response.V1;

namespace Quotes.Application.Models.Results
{
    public sealed record CreateQuoteResult
    {
        public required QuoteResponse Quote { get; init; }

        /// <summary>
        /// True when this was a repeat of an earlier submission and the original quote was returned.
        /// </summary>
        public required bool IsReplay { get; init; }

        /// <summary>
        /// How duplicates were detected for this submission: the client's key, or a fingerprint of the body.
        /// </summary>
        public required IdempotencyKeySource KeySource { get; init; }
    }
}
