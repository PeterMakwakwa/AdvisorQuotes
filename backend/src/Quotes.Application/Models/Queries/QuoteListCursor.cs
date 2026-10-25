using System.Buffers.Text;
using System.Globalization;
using System.Text;

namespace Quotes.Application.Models.Queries
{
    /// <summary>
    /// Position in the newest-first quote list: the last quote of the previous page. Opaque to clients (base64url), so the
    /// paging strategy can change without changing the API.
    /// </summary>
    public sealed record QuoteListCursor
    {
        private const char Separator = ':';

        public required DateTimeOffset CreatedAt { get; init; }

        public required Guid Id { get; init; }

        public string Encode()
        {
            string position = string.Create(CultureInfo.InvariantCulture, $"{CreatedAt.UtcTicks}{Separator}{Id:N}");

            return Base64Url.EncodeToString(Encoding.UTF8.GetBytes(position));
        }

        public static bool TryDecode(string? value, out QuoteListCursor? cursor)
        {
            cursor = null;

            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            try
            {
                string[] parts = Encoding.UTF8.GetString(Base64Url.DecodeFromChars(value)).Split(Separator);

                if (parts.Length == 2
                    && long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out long ticks)
                    && ticks <= DateTimeOffset.MaxValue.UtcTicks
                    && Guid.TryParseExact(parts[1], "N", out Guid id))
                {
                    cursor = new QuoteListCursor { CreatedAt = new DateTimeOffset(ticks, TimeSpan.Zero), Id = id };

                    return true;
                }
            }
            catch (FormatException)
            {
                // Not base64url: treated as an invalid cursor below.
            }

            return false;
        }
    }
}
