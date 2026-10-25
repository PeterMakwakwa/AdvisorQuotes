using FluentValidation;
using Quotes.Application.Constants;
using Quotes.Application.Models.Queries;
using Quotes.Domain.Observability.Messages;

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
                .WithMessage(ResponseMessages.CursorInvalid);

            RuleFor(query => query.PageSize)
                .InclusiveBetween(1, ListQuotesQuery.MaxPageSize);
        }
    }
}
