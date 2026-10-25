using System.Security.Cryptography;
using System.Text.Json;

namespace Quotes.Application.Idempotency
{
    internal static class RequestHasher
    {
        /// <summary>
        /// SHA-256 of the request serialised as JSON. Two submissions with the same content produce the same hash.
        /// </summary>
        public static string Compute<TRequest>(TRequest request)
        {
            byte[] payload = JsonSerializer.SerializeToUtf8Bytes(request);

            return Convert.ToHexStringLower(SHA256.HashData(payload));
        }
    }
}
