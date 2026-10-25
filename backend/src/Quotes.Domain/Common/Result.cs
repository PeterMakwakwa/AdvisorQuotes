namespace Quotes.Domain.Common
{
    public class Result
    {
        protected Result(bool isSuccess, Error? error)
        {
            IsSuccess = isSuccess;
            Error = error;
        }

        public bool IsSuccess { get; }

        public bool IsFailure => !IsSuccess;

        public Error? Error { get; }

        public static Result Success()
        {
            return new Result(true, null);
        }

        public static Result Failure(Error error)
        {
            return new Result(false, error);
        }
    }

    public sealed class Result<T> : Result
    {
        private readonly T? _value;

        private Result(T value)
            : base(true, null)
        {
            _value = value;
        }

        private Result(Error error)
            : base(false, error)
        {
        }

        public T Value => IsSuccess
            ? _value!
            : throw new InvalidOperationException("Cannot read the value of a failed result.");

        // A value or an Error converts implicitly, so callers write "return quote;" or "return QuoteErrors.NotFound(id);".
        public static implicit operator Result<T>(T value)
        {
            return new Result<T>(value);
        }

        public static implicit operator Result<T>(Error error)
        {
            return new Result<T>(error);
        }
    }
}
