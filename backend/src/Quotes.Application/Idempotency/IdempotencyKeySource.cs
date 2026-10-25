namespace Quotes.Application.Idempotency
{
    public enum IdempotencyKeySource
    {
        /// <summary>
        /// The client sent an Idempotency-Key header.
        /// </summary>
        Client,

        /// <summary>
        /// No header: the key is a fingerprint of the normalised request body.
        /// </summary>
        Fingerprint
    }
}
