using Quotes.Infrastructure.Messaging;
using Shouldly;

namespace Quotes.IntegrationTests
{
    public class OutboxRetryPolicyTests
    {
        private static readonly TimeSpan Initial = TimeSpan.FromSeconds(5);

        private static readonly TimeSpan Max = TimeSpan.FromMinutes(5);

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 5)]
        [InlineData(2, 10)]
        [InlineData(3, 20)]
        [InlineData(6, 160)]
        [InlineData(7, 300)]
        [InlineData(50, 300)]
        public void DelayAfter_DoublesEachAttemptUpToTheMaximum(int failedAttempts, int expectedSeconds)
        {
            OutboxRetryPolicy.DelayAfter(failedAttempts, Initial, Max).ShouldBe(TimeSpan.FromSeconds(expectedSeconds));
        }

        [Fact]
        public void DelayAfter_ZeroInitialDelay_RetriesImmediately()
        {
            OutboxRetryPolicy.DelayAfter(5, TimeSpan.Zero, Max).ShouldBe(TimeSpan.Zero);
        }
    }
}
