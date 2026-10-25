using Quotes.Application.Abstractions;
using Quotes.Application.Interfaces;
using Quotes.Application.Models.Queries;
using Quotes.Application.Models.Response.V1;
using Quotes.Domain.Common;
using Quotes.Domain.Errors;

namespace Quotes.Application.Handlers
{
    public sealed class GetQuoteHandler : IQueryHandler<GetQuoteQuery, QuoteResponse>
    {
        private readonly IQuoteReadService _quoteReadService;

        public GetQuoteHandler(IQuoteReadService quoteReadService)
        {
            _quoteReadService = quoteReadService;
        }

        public async Task<Result<QuoteResponse>> HandleAsync(GetQuoteQuery query, CancellationToken cancellationToken)
        {
            QuoteResponse? quote = await _quoteReadService.GetByIdAsync(query.QuoteId, cancellationToken);

            if (quote is null)
            {
                return QuoteErrors.NotFound(query.QuoteId);
            }

            return quote;
        }
    }
}
