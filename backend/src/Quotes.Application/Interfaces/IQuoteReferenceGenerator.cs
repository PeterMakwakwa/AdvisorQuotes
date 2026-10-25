namespace Quotes.Application.Interfaces
{
    /// <summary>
    /// Produces the human-readable external reference for a new quote (for example "Q-2026-000123"). The year is the year
    /// the quote was captured; the number comes from one sequence for all years, so it does not restart in January
    /// (Q-2027-000124 can follow Q-2026-000123). References are unique, increasing and may have gaps.
    /// </summary>
    public interface IQuoteReferenceGenerator
    {
        Task<string> NextAsync(CancellationToken cancellationToken);
    }
}
