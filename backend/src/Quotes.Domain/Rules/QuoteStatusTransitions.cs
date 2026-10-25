using System.Collections.Frozen;
using Quotes.Domain.Enums;

namespace Quotes.Domain.Rules
{
    public static class QuoteStatusTransitions
    {
        // Frozen: built once, read-only and optimised for lookups.
        private static readonly FrozenDictionary<QuoteStatus, QuoteStatus[]> Allowed =
            new Dictionary<QuoteStatus, QuoteStatus[]>
            {
                [QuoteStatus.Draft] = [QuoteStatus.SentToClient, QuoteStatus.Declined],
                [QuoteStatus.SentToClient] = [QuoteStatus.Accepted, QuoteStatus.Declined],
                [QuoteStatus.Accepted] = [],
                [QuoteStatus.Declined] = []
            }.ToFrozenDictionary();

        /// <summary>
        /// Statuses a quote can still move on from: it is still in progress with the client.
        /// </summary>
        public static IReadOnlyList<QuoteStatus> OpenStatuses { get; } =
            Allowed.Where(entry => entry.Value.Length > 0).Select(entry => entry.Key).Order().ToArray();

        public static bool CanTransition(QuoteStatus from, QuoteStatus to)
        {
            return Allowed.TryGetValue(from, out QuoteStatus[]? next) && next.Contains(to);
        }

        public static IReadOnlyList<QuoteStatus> AllowedFrom(QuoteStatus from)
        {
            return Allowed.TryGetValue(from, out QuoteStatus[]? next) ? next : [];
        }
    }
}
