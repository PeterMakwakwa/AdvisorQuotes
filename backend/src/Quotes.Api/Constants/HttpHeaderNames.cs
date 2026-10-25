namespace Quotes.Api.Constants
{
    public static class HttpHeaderNames
    {
        /// <summary>
        /// Sent by the client to make a create request safe to retry.
        /// </summary>
        public const string IdempotencyKey = "Idempotency-Key";

        /// <summary>
        /// Returned as "true" when the response is the original quote for a repeated submission.
        /// </summary>
        public const string IdempotentReplayed = "Idempotent-Replayed";

        /// <summary>
        /// The W3C trace id of the request, so an operator can find its trace and logs.
        /// </summary>
        public const string TraceId = "X-Trace-Id";
    }
}
