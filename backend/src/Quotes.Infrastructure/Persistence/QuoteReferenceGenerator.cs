using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Quotes.Application.Interfaces;

namespace Quotes.Infrastructure.Persistence
{
    /// <summary>
    /// Builds "Q-{year}-{number}" from the quote_reference_seq sequence (src/dbQuotes/up/0004).
    /// </summary>
    internal sealed class QuoteReferenceGenerator : IQuoteReferenceGenerator
    {
        private readonly QuotesDbContext _dbContext;

        private readonly TimeProvider _timeProvider;

        public QuoteReferenceGenerator(QuotesDbContext dbContext, TimeProvider timeProvider)
        {
            _dbContext = dbContext;
            _timeProvider = timeProvider;
        }

        public async Task<string> NextAsync(CancellationToken cancellationToken)
        {
            long next = await _dbContext.Database
                .SqlQuery<long>($"SELECT nextval('quote_reference_seq') AS \"Value\"")
                .SingleAsync(cancellationToken);

            int year = _timeProvider.GetUtcNow().Year;

            return string.Create(CultureInfo.InvariantCulture, $"Q-{year}-{next:D6}");
        }
    }
}
