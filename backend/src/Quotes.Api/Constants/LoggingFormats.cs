namespace Quotes.Api.Constants
{
    /// <summary>
    /// Values of Logging:Format and the console template used for the text format.
    /// </summary>
    public static class LoggingFormats
    {
        public const string Text = "text";

        public const string Json = "json";

        public const string TextTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] trace={TraceId} {SourceContext}: {Message:lj}{NewLine}{Exception}";
    }
}
