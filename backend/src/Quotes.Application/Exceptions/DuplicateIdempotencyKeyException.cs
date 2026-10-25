namespace Quotes.Application.Exceptions
{
    /// <summary>
    /// Thrown by the unit of work when another request saved the same idempotency key first (the unique constraint was hit).
    /// </summary>
    public sealed class DuplicateIdempotencyKeyException : Exception
    {
        public DuplicateIdempotencyKeyException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
