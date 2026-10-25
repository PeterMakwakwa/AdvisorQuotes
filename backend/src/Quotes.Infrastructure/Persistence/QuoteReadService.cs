using Microsoft.EntityFrameworkCore;
using Quotes.Application.Interfaces;
using Quotes.Application.Models.Queries;
using Quotes.Application.Models.Response.V1;
using Quotes.Domain.Entities;
using Quotes.Domain.Enums;
using Quotes.Domain.Rules;

namespace Quotes.Infrastructure.Persistence
{
    /// <summary>
    /// The query side. Both queries project straight from the tables into response models: the Quote aggregate is
    /// never loaded or tracked here, and nothing is written. Changes only happen through the aggregate (IQuoteRepository).
    /// </summary>
    internal sealed class QuoteReadService : IQuoteReadService
    {
        private readonly QuotesDbContext _dbContext;

        public QuoteReadService(QuotesDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<QuoteResponse?> GetByIdAsync(Guid quoteId, CancellationToken cancellationToken)
        {
            QuoteResponse? response = await _dbContext.Quotes
                .AsNoTracking()
                .Where(quote => quote.Id == quoteId)
                .Select(quote => new QuoteResponse
                {
                    Id = quote.Id,
                    ExternalReference = quote.ExternalReference,
                    AdviserId = quote.AdviserId,
                    AdviserReference = quote.AdviserReference,
                    Client = new ClientResponse
                    {
                        FullName = quote.Client.FullName,
                        DateOfBirth = quote.Client.DateOfBirth,
                        IsSmoker = quote.Client.IsSmoker
                    },
                    Product = new ProductResponse
                    {
                        ProductCode = quote.Product.ProductCode,
                        CoverAmount = quote.Product.CoverAmount,
                        TermYears = quote.Product.TermYears
                    },
                    Premium = new PremiumResponse
                    {
                        MonthlyAmount = quote.MonthlyPremium.Amount,
                        Currency = quote.MonthlyPremium.Currency,
                        PricingVersion = quote.PricingVersion
                    },
                    Status = quote.Status,
                    AllowedNextStatuses = Array.Empty<QuoteStatus>(),
                    Notes = quote.Notes,
                    CreatedAt = quote.CreatedAt,
                    UpdatedAt = quote.UpdatedAt,
                    StatusHistory = quote.StatusHistory
                        .OrderBy(change => change.ChangedAt)
                        .ThenBy(change => change.Id)
                        .Select(change => new StatusChangeResponse
                        {
                            FromStatus = change.FromStatus,
                            ToStatus = change.ToStatus,
                            Reason = change.Reason,
                            ChangedBy = change.ChangedBy,
                            ChangedAt = change.ChangedAt
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);

            // The allowed next statuses are a domain rule, not data, so they are added after the query
            // from the same transition table the aggregate uses.
            return response is null
                ? null
                : response with { AllowedNextStatuses = QuoteStatusTransitions.AllowedFrom(response.Status) };
        }

        public async Task<PagedResponse<QuoteSummaryResponse>> ListAsync(
            ListQuotesQuery query,
            CancellationToken cancellationToken)
        {
            IQueryable<Quote> quotes = _dbContext.Quotes.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query.AdviserId))
            {
                string adviserId = query.AdviserId.Trim();
                quotes = quotes.Where(quote => quote.AdviserId == adviserId);
            }

            if (!string.IsNullOrWhiteSpace(query.AdviserReference))
            {
                string adviserReference = query.AdviserReference.Trim();
                quotes = quotes.Where(quote => quote.AdviserReference == adviserReference);
            }

            if (query.Status is not null)
            {
                quotes = quotes.Where(quote => quote.Status == query.Status);
            }

            if (!string.IsNullOrWhiteSpace(query.ClientName))
            {
                // Names are stored trimmed. upper(client_name) = upper(@search) is an exact match ignoring case, answered
                // by the ix_quotes_upper_client_name_date_of_birth expression index. PostgreSQL upper-cases both sides,
                // so the stored name and the search follow the same case rules.
                string clientName = query.ClientName.Trim();
                quotes = quotes.Where(quote => PostgresFunctions.Upper(quote.Client.FullName) == PostgresFunctions.Upper(clientName));
            }

            if (query.ClientDateOfBirth is not null)
            {
                DateOnly dateOfBirth = query.ClientDateOfBirth.Value;
                quotes = quotes.Where(quote => quote.Client.DateOfBirth == dateOfBirth);
            }

            if (!string.IsNullOrWhiteSpace(query.ProductCode))
            {
                string productCode = query.ProductCode.Trim().ToUpperInvariant();
                quotes = quotes.Where(quote => quote.Product.ProductCode == productCode);
            }

            if (query.OpenOnly)
            {
                QuoteStatus[] openStatuses = [.. QuoteStatusTransitions.OpenStatuses];
                quotes = quotes.Where(quote => openStatuses.Contains(quote.Status));
            }

            // The total only comes with the first page: counting every match again for each later page would make deep
            // paging as expensive as the count itself.
            bool isFirstPage = !QuoteListCursor.TryDecode(query.Cursor, out QuoteListCursor? cursor);
            int? totalCount = isFirstPage ? await quotes.CountAsync(cancellationToken) : null;

            if (!isFirstPage)
            {
                // Keyset paging: everything after the last quote of the previous page, as the row comparison
                // (created_at, id) < (cursor.CreatedAt, cursor.Id), which the (created_at DESC, id DESC) index answers directly.
                DateTimeOffset createdAt = cursor!.CreatedAt;
                Guid id = cursor.Id;
                quotes = quotes.Where(quote =>
                    EF.Functions.LessThan(ValueTuple.Create(quote.CreatedAt, quote.Id), ValueTuple.Create(createdAt, id)));
            }

            // One extra row tells us whether there is a next page without a second query.
            List<QuoteSummaryResponse> rows = await quotes
                .OrderByDescending(quote => quote.CreatedAt)
                .ThenByDescending(quote => quote.Id)
                .Take(query.PageSize + 1)
                .Select(quote => new QuoteSummaryResponse
                {
                    Id = quote.Id,
                    ExternalReference = quote.ExternalReference,
                    AdviserId = quote.AdviserId,
                    AdviserReference = quote.AdviserReference,
                    ClientName = quote.Client.FullName,
                    ProductCode = quote.Product.ProductCode,
                    CoverAmount = quote.Product.CoverAmount,
                    MonthlyPremium = quote.MonthlyPremium.Amount,
                    Currency = quote.MonthlyPremium.Currency,
                    Status = quote.Status,
                    CreatedAt = quote.CreatedAt,
                    UpdatedAt = quote.UpdatedAt
                })
                .ToListAsync(cancellationToken);

            List<QuoteSummaryResponse> items = rows.Take(query.PageSize).ToList();
            QuoteSummaryResponse? last = rows.Count > query.PageSize ? items[^1] : null;

            return new PagedResponse<QuoteSummaryResponse>
            {
                Items = items,
                PageSize = query.PageSize,
                TotalCount = totalCount,
                NextCursor = last is null ? null : new QuoteListCursor { CreatedAt = last.CreatedAt, Id = last.Id }.Encode()
            };
        }
    }
}
