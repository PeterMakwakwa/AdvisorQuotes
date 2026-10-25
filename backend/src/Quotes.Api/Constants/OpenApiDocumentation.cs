namespace Quotes.Api.Constants
{
    /// <summary>
    /// Text shown in the OpenAPI document and the Scalar reference UI.
    /// </summary>
    public static class OpenApiDocumentation
    {
        public const string Title = "Advisor Quotes API";

        public const string Description =
            "Capture, price, view and progress insurance quotes. Errors are RFC 9457 problem details with a stable `code`; "
            + "every response carries `X-Trace-Id` for following the request in traces and logs.";

        public const string IdempotencyKeyDescription =
            "Optional key that makes the request safe to retry: a repeat with the same key and body returns the original quote "
            + "(header Idempotent-Replayed: true); the same key with a different body is rejected with 422. "
            + "Without a key, an identical body from the same adviser within a short window is treated as a duplicate.";
    }
}
