namespace Quotes.Api.Constants
{
    public static class MediaTypes
    {
        public const string Json = "application/json";

        /// <summary>
        /// RFC 9457 problem details, used for every error response.
        /// </summary>
        public const string ProblemJson = "application/problem+json";
    }
}
