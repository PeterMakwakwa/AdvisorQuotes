using Quotes.Application.Idempotency;
using Quotes.Application.Interfaces;
using Quotes.Application.Models.Queries;
using Quotes.Application.Models.Response.V1;
using Quotes.Domain.Entities;
using Quotes.Domain.Repositories;
using Quotes.Domain.Rules;

namespace Quotes.UnitTests.Fakes
{
    /// <summary>
    /// Stands in for the database: added items stay pending until SaveChangesAsync, like a real unit of work,
    /// and a failed save discards them, like a rolled-back transaction.
    /// </summary>
    internal sealed class InMemoryQuoteStore : IQuoteRepository, IQuoteReadService, IIdempotencyStore, IUnitOfWork
    {
        private readonly List<Quote> _pendingQuotes = [];

        private readonly List<IdempotencyRecord> _pendingRecords = [];

        private Exception? _nextSaveException;

        private Action? _beforeFailedSave;

        public List<Quote> Quotes { get; } = [];

        public List<IdempotencyRecord> IdempotencyRecords { get; } = [];

        public int SaveCount { get; private set; }

        public void FailNextSaveWith(Exception exception, Action? beforeFailing = null)
        {
            _nextSaveException = exception;
            _beforeFailedSave = beforeFailing;
        }

        Task<Quote?> IQuoteRepository.GetByIdAsync(Guid quoteId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Quotes.FirstOrDefault(quote => quote.Id == quoteId));
        }

        public void Add(Quote quote)
        {
            _pendingQuotes.Add(quote);
        }

        Task<QuoteResponse?> IQuoteReadService.GetByIdAsync(Guid quoteId, CancellationToken cancellationToken)
        {
            Quote? quote = Quotes.FirstOrDefault(candidate => candidate.Id == quoteId);

            return Task.FromResult(quote is null ? null : QuoteResponse.FromDomain(quote));
        }

        public Task<PagedResponse<QuoteSummaryResponse>> ListAsync(ListQuotesQuery query, CancellationToken cancellationToken)
        {
            List<Quote> matches = Quotes
                .Where(quote => query.AdviserId is null || quote.AdviserId == query.AdviserId)
                .Where(quote => query.AdviserReference is null || quote.AdviserReference == query.AdviserReference)
                .Where(quote => query.Status is null || quote.Status == query.Status)
                .Where(quote => string.IsNullOrWhiteSpace(query.ClientName)
                    || string.Equals(quote.Client.FullName, query.ClientName.Trim(), StringComparison.OrdinalIgnoreCase))
                .Where(quote => query.ClientDateOfBirth is null || quote.Client.DateOfBirth == query.ClientDateOfBirth)
                .Where(quote => string.IsNullOrWhiteSpace(query.ProductCode)
                    || string.Equals(quote.Product.ProductCode, query.ProductCode.Trim(), StringComparison.OrdinalIgnoreCase))
                .Where(quote => !query.OpenOnly || QuoteStatusTransitions.OpenStatuses.Contains(quote.Status))
                .OrderByDescending(quote => quote.CreatedAt)
                .ThenByDescending(quote => quote.Id)
                .ToList();

            IEnumerable<Quote> remaining = matches;

            if (QuoteListCursor.TryDecode(query.Cursor, out QuoteListCursor? cursor))
            {
                remaining = matches.Where(quote => quote.CreatedAt < cursor!.CreatedAt
                    || (quote.CreatedAt == cursor.CreatedAt && quote.Id.CompareTo(cursor.Id) < 0));
            }

            List<Quote> rows = remaining.Take(query.PageSize + 1).ToList();
            List<QuoteSummaryResponse> items = rows.Take(query.PageSize).Select(QuoteSummaryResponse.FromDomain).ToList();
            QuoteSummaryResponse? last = rows.Count > query.PageSize ? items[^1] : null;

            PagedResponse<QuoteSummaryResponse> page = new PagedResponse<QuoteSummaryResponse>
            {
                Items = items,
                PageSize = query.PageSize,
                TotalCount = matches.Count,
                NextCursor = last is null ? null : new QuoteListCursor { CreatedAt = last.CreatedAt, Id = last.Id }.Encode()
            };

            return Task.FromResult(page);
        }

        public Task<IdempotencyRecord?> FindAsync(string adviserId, string key, CancellationToken cancellationToken)
        {
            return Task.FromResult(IdempotencyRecords.FirstOrDefault(record => record.AdviserId == adviserId && record.Key == key));
        }

        public void Add(IdempotencyRecord record)
        {
            _pendingRecords.Add(record);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            if (_nextSaveException is not null)
            {
                Exception exception = _nextSaveException;
                _nextSaveException = null;
                _pendingQuotes.Clear();
                _pendingRecords.Clear();
                _beforeFailedSave?.Invoke();

                throw exception;
            }

            Quotes.AddRange(_pendingQuotes);
            IdempotencyRecords.AddRange(_pendingRecords);
            _pendingQuotes.Clear();
            _pendingRecords.Clear();
            SaveCount++;

            return Task.CompletedTask;
        }
    }
}
