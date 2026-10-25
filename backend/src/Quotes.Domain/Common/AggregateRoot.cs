namespace Quotes.Domain.Common
{
    /// <summary>
    /// Base for aggregate roots. Only the aggregate's own methods raise events; infrastructure saves them to the outbox
    /// in the same transaction as the aggregate, then clears them.
    /// </summary>
    public abstract class AggregateRoot
    {
        private readonly List<IDomainEvent> _domainEvents = [];

        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }

        protected void Raise(IDomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }
    }
}
