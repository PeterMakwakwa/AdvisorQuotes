using Quotes.Application.Constants;
using Quotes.Domain.Common;

namespace Quotes.Application.Errors
{
    public static class IdempotencyErrors
    {
        public static readonly Error KeyTooLong = Error.Validation(
            "Idempotency.KeyTooLong",
            $"The Idempotency-Key header must be at most {FieldLengths.IdempotencyKey} characters.");

        public static readonly Error KeyReusedWithDifferentRequest = Error.Unprocessable(
            "Idempotency.KeyReused",
            "This Idempotency-Key was already used with a different request body.");
    }
}
