namespace Quotes.Api.Constants
{
    public static class RateLimitingPolicies
    {
        /// <summary>
        /// The fixed window per client applied to the quote endpoints.
        /// </summary>
        public const string Quotes = "quotes";

        /// <summary>
        /// The partition for requests whose client address is not known.
        /// </summary>
        public const string UnknownClient = "unknown";
    }
}
