namespace Quotes.Application.Interfaces
{
    /// <summary>
    /// Produces the human-readable external reference for a new quote (for example "Q-2026-000123").
    /// </summary>
    public interface IQuoteReferenceGenerator
    {
        Task<string> NextAsync(CancellationToken cancellationToken);
    }
}
