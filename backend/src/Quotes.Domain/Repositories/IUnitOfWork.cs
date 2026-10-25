namespace Quotes.Domain.Repositories
{
    /// <summary>
    /// Commits every pending change (quotes, status history, idempotency records) in one database transaction.
    /// </summary>
    public interface IUnitOfWork
    {
        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}
