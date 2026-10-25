namespace Quotes.Domain.Common
{
    /// <summary>
    /// Something that happened to an aggregate, raised by the aggregate itself.
    /// </summary>
    public interface IDomainEvent
    {
        /// <summary>
        /// Unique per event: consumers use it to ignore a delivery they have already processed.
        /// </summary>
        Guid EventId { get; }

        DateTimeOffset OccurredAt { get; }
    }
}
