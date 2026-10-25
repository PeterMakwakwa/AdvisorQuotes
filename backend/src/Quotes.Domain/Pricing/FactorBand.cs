namespace Quotes.Domain.Pricing
{
    /// <summary>
    /// A rating band: any value from <see cref="From"/> to <see cref="To"/> (both inclusive) is multiplied by <see cref="Factor"/>.
    /// </summary>
    public sealed record FactorBand
    {
        public required int From { get; init; }

        public required int To { get; init; }

        public required decimal Factor { get; init; }

        public bool Contains(int value)
        {
            return value >= From && value <= To;
        }
    }
}
