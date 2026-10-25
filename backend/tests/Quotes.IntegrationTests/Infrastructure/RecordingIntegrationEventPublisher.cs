using System.Collections.Concurrent;
using Quotes.Infrastructure.Messaging;

namespace Quotes.IntegrationTests.Infrastructure
{
    /// <summary>
    /// Stands in for the message broker: records what was published, can simulate an outage for chosen messages, and can
    /// hold a publish open (a "slow broker") so a test can inspect the database while publishing is in progress.
    /// </summary>
    public sealed class RecordingIntegrationEventPublisher : IIntegrationEventPublisher
    {
        private readonly ConcurrentQueue<OutboxMessage> _published = new ConcurrentQueue<OutboxMessage>();

        private volatile string? _failWhenPayloadContains;

        private volatile PublishGate? _gate;

        public IReadOnlyCollection<OutboxMessage> Published => _published.ToArray();

        public void FailWhenPayloadContains(string? text)
        {
            _failWhenPayloadContains = text;
        }

        /// <summary>
        /// The next publish of a message whose payload contains <paramref name="text"/> waits until the gate is released.
        /// </summary>
        public PublishGate HoldWhenPayloadContains(string text)
        {
            PublishGate gate = new PublishGate(text);
            _gate = gate;

            return gate;
        }

        public async Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken)
        {
            PublishGate? gate = _gate;

            if (gate is not null && message.Payload.Contains(gate.Match, StringComparison.Ordinal))
            {
                _gate = null;
                gate.SignalEntered();
                await gate.WaitForReleaseAsync(cancellationToken);
            }

            string? failure = _failWhenPayloadContains;

            if (failure is not null && message.Payload.Contains(failure, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Broker unavailable (simulated by the test).");
            }

            _published.Enqueue(message);
        }
    }

    public sealed class PublishGate
    {
        private readonly TaskCompletionSource _entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly TaskCompletionSource _released = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        internal PublishGate(string match)
        {
            Match = match;
        }

        internal string Match { get; }

        /// <summary>
        /// Completes once the publisher is holding the message.
        /// </summary>
        public Task WaitUntilHeldAsync()
        {
            return _entered.Task.WaitAsync(TimeSpan.FromSeconds(30));
        }

        public void Release()
        {
            _released.TrySetResult();
        }

        internal void SignalEntered()
        {
            _entered.TrySetResult();
        }

        internal Task WaitForReleaseAsync(CancellationToken cancellationToken)
        {
            return _released.Task.WaitAsync(cancellationToken);
        }
    }
}
