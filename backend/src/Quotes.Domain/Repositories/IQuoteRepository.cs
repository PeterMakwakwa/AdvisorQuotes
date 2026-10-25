using Quotes.Domain.Entities;

namespace Quotes.Domain.Repositories
{
    /// <summary>
    /// Loads and stores the Quote aggregate (with its status history) for changes. Reads for display go through IQuoteReadService.
    /// </summary>
    public interface IQuoteRepository
    {
        Task<Quote?> GetByIdAsync(Guid quoteId, CancellationToken cancellationToken);

        void Add(Quote quote);
    }
}
