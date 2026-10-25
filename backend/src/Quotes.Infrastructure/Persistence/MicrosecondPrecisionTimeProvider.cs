namespace Quotes.Infrastructure.Persistence
{
    /// <summary>
    /// PostgreSQL timestamptz stores microseconds, .NET ticks are 100 ns. Truncating "now" to microseconds at the source
    /// means a value returned straight after a save equals the value read back later (create vs replay vs GET).
    /// A decorator, so the Domain and Application stay unaware of the database's precision.
    /// </summary>
    public sealed class MicrosecondPrecisionTimeProvider : TimeProvider
    {
        private readonly TimeProvider _inner;

        public MicrosecondPrecisionTimeProvider(TimeProvider inner)
        {
            _inner = inner;
        }

        public override TimeZoneInfo LocalTimeZone => _inner.LocalTimeZone;

        public override long TimestampFrequency => _inner.TimestampFrequency;

        public override DateTimeOffset GetUtcNow()
        {
            DateTimeOffset now = _inner.GetUtcNow();

            return now.AddTicks(-(now.Ticks % TimeSpan.TicksPerMicrosecond));
        }

        public override long GetTimestamp()
        {
            return _inner.GetTimestamp();
        }

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            return _inner.CreateTimer(callback, state, dueTime, period);
        }
    }
}
