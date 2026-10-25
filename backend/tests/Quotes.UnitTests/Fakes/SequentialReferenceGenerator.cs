using Quotes.Application.Interfaces;

namespace Quotes.UnitTests.Fakes
{
    internal sealed class SequentialReferenceGenerator : IQuoteReferenceGenerator
    {
        private int _next = 1;

        public Task<string> NextAsync(CancellationToken cancellationToken)
        {
            string reference = $"Q-2026-{_next:D6}";
            _next++;

            return Task.FromResult(reference);
        }
    }
}
