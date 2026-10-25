using Microsoft.EntityFrameworkCore;
using Quotes.Domain.Entities;
using Quotes.Domain.Repositories;

namespace Quotes.Infrastructure.Persistence.Repositories
{
    internal sealed class QuoteRepository : IQuoteRepository
    {
        private readonly QuotesDbContext _dbContext;

        public QuoteRepository(QuotesDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<Quote?> GetByIdAsync(Guid quoteId, CancellationToken cancellationToken)
        {
            return _dbContext.Quotes
                .Include(quote => quote.StatusHistory)
                .FirstOrDefaultAsync(quote => quote.Id == quoteId, cancellationToken);
        }

        public void Add(Quote quote)
        {
            _dbContext.Quotes.Add(quote);
        }
    }
}
