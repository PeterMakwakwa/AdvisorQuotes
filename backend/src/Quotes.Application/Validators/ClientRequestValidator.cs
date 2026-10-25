using FluentValidation;
using Quotes.Application.Constants;
using Quotes.Application.Models.Request.V1;
using Quotes.Domain.Observability.Messages;

namespace Quotes.Application.Validators
{
    public sealed class ClientRequestValidator : AbstractValidator<ClientRequest>
    {
        private static readonly DateOnly EarliestDateOfBirth = new DateOnly(1900, 1, 1);

        public ClientRequestValidator(TimeProvider timeProvider)
        {
            RuleFor(client => client.FullName)
                .NotEmpty()
                .MaximumLength(FieldLengths.ClientName);

            RuleFor(client => client.DateOfBirth)
                .Must(dateOfBirth => dateOfBirth < DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime))
                .WithMessage(ResponseMessages.DateOfBirthNotInPast)
                .GreaterThan(EarliestDateOfBirth)
                .WithMessage(ResponseMessages.DateOfBirthInvalid);
        }
    }
}
