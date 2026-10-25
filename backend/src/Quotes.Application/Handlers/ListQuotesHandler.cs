using FluentValidation;
using FluentValidation.Results;
using Quotes.Application.Abstractions;
using Quotes.Application.Common;
using Quotes.Application.Interfaces;
using Quotes.Application.Models.Queries;
using Quotes.Application.Models.Response.V1;
using Quotes.Domain.Common;

namespace Quotes.Application.Handlers
{
    public sealed class ListQuotesHandler : IQueryHandler<ListQuotesQuery, PagedResponse<QuoteSummaryResponse>>
    {
        private readonly IValidator<ListQuotesQuery> _validator;

        private readonly IQuoteReadService _quoteReadService;

        public ListQuotesHandler(IValidator<ListQuotesQuery> validator, IQuoteReadService quoteReadService)
        {
            _validator = validator;
            _quoteReadService = quoteReadService;
        }

        public async Task<Result<PagedResponse<QuoteSummaryResponse>>> HandleAsync(
            ListQuotesQuery query,
            CancellationToken cancellationToken)
        {
            ValidationResult validation = await _validator.ValidateAsync(query, cancellationToken);

            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            return await _quoteReadService.ListAsync(query, cancellationToken);
        }
    }
}
