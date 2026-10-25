using System.Text.Json;
using System.Text.Json.Serialization;
using Quotes.Domain.Common;
using Quotes.Domain.Events;

namespace Quotes.Infrastructure.Messaging
{
    /// <summary>
    /// Turns domain events into versioned integration messages. The type name is the public contract; adding a field is
    /// backwards compatible, while a breaking change gets a new version (v2) published alongside the old one.
    /// </summary>
    public static class IntegrationEvents
    {
        public const string QuoteCreatedType = "quotes.quote-created.v1";

        public const string QuoteStatusChangedType = "quotes.quote-status-changed.v1";

        private static readonly JsonSerializerOptions PayloadOptions = CreatePayloadOptions();

        internal static OutboxMessage ToOutboxMessage(IDomainEvent domainEvent, string? traceParent)
        {
            string eventType = domainEvent switch
            {
                QuoteCreated => QuoteCreatedType,
                QuoteStatusChanged => QuoteStatusChangedType,
                _ => throw new InvalidOperationException($"No integration event type is registered for {domainEvent.GetType().Name}.")
            };

            string payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), PayloadOptions);

            return OutboxMessage.Create(domainEvent.EventId, eventType, payload, domainEvent.OccurredAt, traceParent);
        }

        private static JsonSerializerOptions CreatePayloadOptions()
        {
            JsonSerializerOptions options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            options.Converters.Add(new JsonStringEnumConverter());

            return options;
        }
    }
}
