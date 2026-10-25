using Quotes.Application.Idempotency;

namespace Quotes.Application.Interfaces
{
    /// <summary>
    /// Idempotency records are saved by the same unit of work as the quote, so both are committed together or not at all.
    /// </summary>
    public interface IIdempotencyStore
    {
        Task<IdempotencyRecord?> FindAsync(string adviserId, string key, CancellationToken cancellationToken);

        void Add(IdempotencyRecord record);
    }
}
