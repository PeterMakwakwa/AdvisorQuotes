namespace Quotes.Application.Exceptions
{
    /// <summary>
    /// Thrown by the unit of work when a row was changed by someone else between loading and saving (optimistic concurrency).
    /// </summary>
    public sealed class ConcurrencyConflictException : Exception
    {
        public ConcurrencyConflictException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
