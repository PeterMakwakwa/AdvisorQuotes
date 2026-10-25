namespace Quotes.Api.Constants
{
    public static class ApiVersions
    {
        /// <summary>
        /// Used for the Location header when the requested version cannot be read.
        /// </summary>
        public const string DefaultMajorVersion = "1";

        /// <summary>
        /// OpenAPI document names: v1, v2, ...
        /// </summary>
        public const string GroupNameFormat = "'v'VVV";
    }
}
