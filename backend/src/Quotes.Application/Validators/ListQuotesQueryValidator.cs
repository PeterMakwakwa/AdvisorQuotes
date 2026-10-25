using FluentValidation;
using Quotes.Application.Constants;
using Quotes.Application.Models.Queries;

namespace Quotes.Application.Validators
{
    public sealed class ListQuotesQueryValidator : AbstractValidator<ListQuotesQuery>
    {
        public ListQuotesQueryValidator()
        {
            RuleFor(query => query.AdviserId)
                .MaximumLength(FieldLengths.AdviserId);

            RuleFor(query => query.AdviserReference)
                .MaximumLength(FieldLengths.AdviserReference);

            RuleFor(query => query.Status)
                .IsInEnum();

            RuleFor(query => query.ClientName)
                .MaximumLength(FieldLengths.ClientName);

            RuleFor(query => query.ProductCode)
                .MaximumLength(FieldLengths.ProductCode);

            RuleFor(query => query.Cursor)
                .Must(cursor => QuoteListCursor.TryDecode(cursor, out _))
                .When(query => query.Cursor is not null)
                .WithMessage("The cursor is not valid. Use the nextCursor value from the previous page.");

            RuleFor(query => query.PageSize)
                .InclusiveBetween(1, ListQuotesQuery.MaxPageSize);
        }
    }
}
