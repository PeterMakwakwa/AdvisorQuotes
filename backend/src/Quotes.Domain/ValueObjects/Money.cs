namespace Quotes.Domain.ValueObjects
{
    public sealed record Money
    {
        private Money()
        {
            // Used by EF Core when loading from the database
        }

        public decimal Amount { get; private init; }

        public string Currency { get; private init; } = string.Empty;

        public static Money Of(decimal amount, string currency)
        {
            return new Money
            {
                Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero),
                Currency = currency.ToUpperInvariant()
            };
        }
    }
}
