using System.Globalization;

namespace Quotes.Domain.Observability.Messages
{
    /// <summary>
    /// Fills the {0}, {1} placeholders of a message constant, always with the invariant culture so the text does not depend
    /// on the server's locale. Messages are built on failure paths only, so the format is not cached.
    /// </summary>
    public static class MessageFormatter
    {
        public static string Format(string message, params object?[] values)
        {
            return string.Format(CultureInfo.InvariantCulture, message, values);
        }
    }
}
