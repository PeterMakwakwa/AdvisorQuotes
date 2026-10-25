using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Quotes.Infrastructure.Messaging
{
    /// <summary>
    /// Trace spans and metrics for publishing outbox messages. The host registers both names with OpenTelemetry.
    /// </summary>
    public sealed class MessagingTelemetry
    {
        public const string SourceName = "Quotes.Infrastructure.Messaging";

        public static readonly ActivitySource ActivitySource = new ActivitySource(SourceName);

        private static readonly IReadOnlyList<double> LagBucketBoundaries = [0.1, 0.5, 1, 2, 5, 10, 30, 60, 300, 900];

        private readonly Counter<long> _published;

        private readonly Counter<long> _failed;

        private readonly Histogram<double> _lag;

        private OutboxBacklog _backlog = OutboxBacklog.Empty;

        public MessagingTelemetry(IMeterFactory meterFactory)
        {
            Meter meter = meterFactory.Create(SourceName);

            // Gauges report the last backlog read (after each dispatcher run, and on each health check), so scraping
            // metrics never queries the database.
            meter.CreateObservableGauge(
                InstrumentNames.Pending,
                () => Volatile.Read(ref _backlog).Pending,
                unit: "{message}",
                description: "Outbox messages waiting to be published.");

            meter.CreateObservableGauge(
                InstrumentNames.DeadLettered,
                () => Volatile.Read(ref _backlog).DeadLettered,
                unit: "{message}",
                description: "Outbox messages that gave up after the maximum attempts and need investigation.");

            meter.CreateObservableGauge(
                InstrumentNames.OldestPendingAge,
                () => Volatile.Read(ref _backlog).OldestPendingAge.TotalSeconds,
                unit: "s",
                description: "How long the oldest pending outbox message has been waiting.");

            _published = meter.CreateCounter<long>(
                InstrumentNames.Published,
                unit: "{message}",
                description: "Outbox messages published.");

            _failed = meter.CreateCounter<long>(
                InstrumentNames.Failed,
                unit: "{message}",
                description: "Outbox publish attempts that failed and will be retried.");

            _lag = meter.CreateHistogram(
                InstrumentNames.Lag,
                unit: "s",
                description: "Time from the event being raised to it being published.",
                tags: null,
                advice: new InstrumentAdvice<double> { HistogramBucketBoundaries = LagBucketBoundaries });
        }

        internal void Published(string eventType, TimeSpan lag)
        {
            TagList tags = new TagList { { TagNames.EventType, eventType } };

            _published.Add(1, tags);
            _lag.Record(lag.TotalSeconds, tags);
        }

        internal void Failed(string eventType)
        {
            _failed.Add(1, new TagList { { TagNames.EventType, eventType } });
        }

        internal void UpdateBacklog(OutboxBacklog backlog)
        {
            Volatile.Write(ref _backlog, backlog);
        }

        public static class InstrumentNames
        {
            public const string Published = "quotes.outbox.published";

            public const string Failed = "quotes.outbox.failed";

            public const string Lag = "quotes.outbox.lag";

            public const string Pending = "quotes.outbox.pending";

            public const string DeadLettered = "quotes.outbox.dead_lettered";

            public const string OldestPendingAge = "quotes.outbox.oldest_pending_age";
        }

        public static class TagNames
        {
            public const string EventType = "messaging.destination.name";

            public const string MessageId = "messaging.message.id";

            public const string System = "messaging.system";

            public const string Operation = "messaging.operation.type";

            public const string Attempt = "messaging.outbox.attempt";
        }
    }
}
