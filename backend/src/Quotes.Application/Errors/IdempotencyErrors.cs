using Quotes.Application.Constants;
using Quotes.Domain.Common;
using Quotes.Domain.Errors;
using Quotes.Domain.Observability.Messages;

namespace Quotes.Application.Errors
{
    public static class IdempotencyErrors
    {
        public static readonly Error KeyTooLong = Error.Validation(
            ErrorCodes.IdempotencyKeyTooLong,
            MessageFormatter.Format(ResponseMessages.IdempotencyKeyTooLong, FieldLengths.IdempotencyKey));

        public static readonly Error KeyReusedWithDifferentRequest = Error.Unprocessable(
            ErrorCodes.IdempotencyKeyReused,
            ResponseMessages.IdempotencyKeyReused);
    }
}
