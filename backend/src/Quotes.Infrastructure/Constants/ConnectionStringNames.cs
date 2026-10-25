namespace Quotes.Infrastructure.Constants
{
    public static class ConnectionStringNames
    {
        /// <summary>
        /// Locally it comes from appsettings.Development.json; elsewhere set the environment variable ConnectionStrings__QuotesDb.
        /// </summary>
        public const string QuotesDb = "QuotesDb";
    }
}
