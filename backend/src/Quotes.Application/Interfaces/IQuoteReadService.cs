using Quotes.Application.Models.Queries;
using Quotes.Application.Models.Response.V1;

namespace Quotes.Application.Interfaces
{
    /// <summary>
    /// The read side: projects straight from the database into response models. It never loads the Quote aggregate
    /// and never changes state; changes go through IQuoteRepository and the aggregate.
    /// </summary>
    public interface IQuoteReadService
    {
        Task<QuoteResponse?> GetByIdAsync(Guid quoteId, CancellationToken cancellationToken);

        Task<PagedResponse<QuoteSummaryResponse>> ListAsync(ListQuotesQuery query, CancellationToken cancellationToken);
    }
}
