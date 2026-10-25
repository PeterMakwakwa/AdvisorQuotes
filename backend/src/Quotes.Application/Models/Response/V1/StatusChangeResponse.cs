using Quotes.Domain.Entities;
using Quotes.Domain.Enums;

namespace Quotes.Application.Models.Response.V1
{
    public sealed record StatusChangeResponse
    {
        public QuoteStatus? FromStatus { get; init; }

        public required QuoteStatus ToStatus { get; init; }

        public string? Reason { get; init; }

        public required string ChangedBy { get; init; }

        public required DateTimeOffset ChangedAt { get; init; }

        public static StatusChangeResponse FromDomain(StatusChange statusChange)
        {
            return new StatusChangeResponse
            {
                FromStatus = statusChange.FromStatus,
                ToStatus = statusChange.ToStatus,
                Reason = statusChange.Reason,
                ChangedBy = statusChange.ChangedBy,
                ChangedAt = statusChange.ChangedAt
            };
        }
    }
}
