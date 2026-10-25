using FluentValidation;
using Quotes.Application.Constants;
using Quotes.Application.Models.Request.V1;

namespace Quotes.Application.Validators
{
    /// <summary>
    /// Checks the request shape. Whether the transition is allowed, and whether a reason is needed, are domain rules on Quote.
    /// </summary>
    public sealed class ChangeQuoteStatusRequestValidator : AbstractValidator<ChangeQuoteStatusRequest>
    {
        public ChangeQuoteStatusRequestValidator()
        {
            RuleFor(request => request.NewStatus)
                .IsInEnum();

            RuleFor(request => request.ChangedBy)
                .NotEmpty()
                .MaximumLength(FieldLengths.ChangedBy);

            RuleFor(request => request.Reason)
                .MaximumLength(FieldLengths.StatusReason);
        }
    }
}
