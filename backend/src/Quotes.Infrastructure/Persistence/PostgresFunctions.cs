using Quotes.Domain.Observability.Messages;

namespace Quotes.Infrastructure.Persistence
{
    /// <summary>
    /// PostgreSQL functions mapped for LINQ (registered in <see cref="QuotesDbContext"/>). A mapped function is always
    /// translated to SQL, even when its argument is a plain .NET value that EF Core would otherwise evaluate in .NET
    /// before sending the query, so both sides of a comparison use the database's rules.
    /// </summary>
    internal static class PostgresFunctions
    {
        /// <summary>
        /// PostgreSQL's upper(). Matches the ix_quotes_upper_client_name_date_of_birth expression index.
        /// </summary>
        public static string Upper(string value)
        {
            throw new NotSupportedException(ErrorMessages.DatabaseFunctionOnly);
        }
    }
}
