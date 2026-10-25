using Quotes.Domain.Commands;
using Quotes.Domain.Enums;

namespace Quotes.Application.Models.Request.V1
{
    /// <summary>
    /// Represents a request to move a quote to a new status.
    /// </summary>
    public sealed record ChangeQuoteStatusRequest
    {
        /// <summary>
        /// The status to move to: SentToClient, Accepted or Declined.
        /// </summary>
        public required QuoteStatus NewStatus { get; init; }

        /// <summary>
        /// Why the status changed. Required when declining.
        /// </summary>
        public string? Reason { get; init; }

        /// <summary>
        /// Who made the change. Authentication is out of scope, so the caller supplies it.
        /// </summary>
        public required string ChangedBy { get; init; }

        public ChangeQuoteStatus BuildCommand()
        {
            return new ChangeQuoteStatus
            {
                NewStatus = NewStatus,
                Reason = Reason,
                ChangedBy = ChangedBy
            };
        }
    }
}
