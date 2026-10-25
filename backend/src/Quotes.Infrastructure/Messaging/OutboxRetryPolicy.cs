namespace Quotes.Infrastructure.Messaging
{
    /// <summary>
    /// Exponential backoff between publish attempts: initial, 2x, 4x, ... capped at the maximum. A broker outage then uses
    /// up attempts over minutes to hours instead of seconds.
    /// </summary>
    public static class OutboxRetryPolicy
    {
        private const int MaxDoublings = 30;

        public static TimeSpan DelayAfter(int failedAttempts, TimeSpan initialDelay, TimeSpan maxDelay)
        {
            if (failedAttempts <= 0 || initialDelay <= TimeSpan.Zero)
            {
                return TimeSpan.Zero;
            }

            double factor = Math.Pow(2, Math.Min(failedAttempts - 1, MaxDoublings));
            double delayTicks = Math.Min(initialDelay.Ticks * factor, maxDelay.Ticks);

            return TimeSpan.FromTicks((long)delayTicks);
        }
    }
}
