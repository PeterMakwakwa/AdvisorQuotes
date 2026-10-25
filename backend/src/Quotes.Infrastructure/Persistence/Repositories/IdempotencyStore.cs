using Microsoft.EntityFrameworkCore;
using Quotes.Application.Idempotency;
using Quotes.Application.Interfaces;

namespace Quotes.Infrastructure.Persistence.Repositories
{
    internal sealed class IdempotencyStore : IIdempotencyStore
    {
        private readonly QuotesDbContext _dbContext;

        public IdempotencyStore(QuotesDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<IdempotencyRecord?> FindAsync(string adviserId, string key, CancellationToken cancellationToken)
        {
            // Tracked on purpose: an expired record is renewed in place and saved with the new quote.
            return _dbContext.IdempotencyRecords
                .FirstOrDefaultAsync(record => record.AdviserId == adviserId && record.Key == key, cancellationToken);
        }

        public void Add(IdempotencyRecord record)
        {
            _dbContext.IdempotencyRecords.Add(record);
        }
    }
}
