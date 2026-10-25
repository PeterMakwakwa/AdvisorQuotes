using System.Diagnostics.CodeAnalysis;

namespace Quotes.Domain.Common
{
    [SuppressMessage(
        "Naming",
        "CA1716:Identifiers should not match keywords",
        Justification = "'Error' is only a keyword in Visual Basic; this service is C#-only and the name is the clearest one.")]
    public sealed record Error
    {
        private const string ValidationFailedCode = "Validation.Failed";

        private const string ValidationFailedMessage = "One or more validation errors occurred.";

        public required string Code { get; init; }

        public required string Message { get; init; }

        public required ErrorType Type { get; init; }

        /// <summary>
        /// Field-level messages, keyed by field path (for example "client.fullName"). Only set for request validation failures.
        /// </summary>
        public IReadOnlyDictionary<string, string[]>? ValidationErrors { get; init; }

        public static Error Validation(string code, string message)
        {
            return new Error { Code = code, Message = message, Type = ErrorType.Validation };
        }

        public static Error Validation(IReadOnlyDictionary<string, string[]> validationErrors)
        {
            return new Error
            {
                Code = ValidationFailedCode,
                Message = ValidationFailedMessage,
                Type = ErrorType.Validation,
                ValidationErrors = validationErrors
            };
        }

        public static Error NotFound(string code, string message)
        {
            return new Error { Code = code, Message = message, Type = ErrorType.NotFound };
        }

        public static Error Conflict(string code, string message)
        {
            return new Error { Code = code, Message = message, Type = ErrorType.Conflict };
        }

        public static Error Unprocessable(string code, string message)
        {
            return new Error { Code = code, Message = message, Type = ErrorType.Unprocessable };
        }
    }
}
