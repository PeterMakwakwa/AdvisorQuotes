using FluentValidation;
using Quotes.Application.Constants;
using Quotes.Application.Models.Request.V1;

namespace Quotes.Application.Validators
{
    public sealed class CreateQuoteRequestValidator : AbstractValidator<CreateQuoteRequest>
    {
        public CreateQuoteRequestValidator(TimeProvider timeProvider)
        {
            RuleFor(request => request.AdviserId)
                .NotEmpty()
                .MaximumLength(FieldLengths.AdviserId);

            RuleFor(request => request.AdviserReference)
                .MaximumLength(FieldLengths.AdviserReference);

            RuleFor(request => request.Client)
                .NotNull()
                .SetValidator(new ClientRequestValidator(timeProvider));

            RuleFor(request => request.Product)
                .NotNull()
                .SetValidator(new ProductRequestValidator());

            RuleFor(request => request.Notes)
                .MaximumLength(FieldLengths.Notes);
        }
    }
}
