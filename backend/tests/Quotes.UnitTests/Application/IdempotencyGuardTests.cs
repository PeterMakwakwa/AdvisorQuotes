using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Quotes.Application.Idempotency;
using Quotes.UnitTests.Fakes;
using Shouldly;

namespace Quotes.UnitTests.Application
{
    /// <summary>
    /// The guard on its own: every decision it can make, independent of quotes.
    /// </summary>
    public class IdempotencyGuardTests
    {
        private const string Scope = "ADV-001";

        private readonly FakeTimeProvider _time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 24, 9, 0, 0, TimeSpan.Zero));

        private readonly InMemoryQuoteStore _store = new InMemoryQuoteStore();

        private readonly IdempotencyGuard _guard;

        public IdempotencyGuardTests()
        {
            _guard = new IdempotencyGuard(_store, Options.Create(new IdempotencyOptions()), _time);
        }

        [Fact]
        public async Task Check_FirstSubmissionWithKey_CanProceedWithTheKeyLifetime()
        {
            IdempotencyCheck check = await _guard.CheckAsync(Scope, " key-1 ", new SampleRequest("a"), CancellationToken.None);

            check.CanProceed.ShouldBeTrue();
            check.Key.ShouldBe("key-1");
            check.KeySource.ShouldBe(IdempotencyKeySource.Client);
            check.Lifetime.ShouldBe(TimeSpan.FromHours(24));
        }

        [Fact]
        public async Task Check_WithoutKey_UsesAFingerprintOfTheBodyAndTheShortWindow()
        {
            IdempotencyCheck first = await _guard.CheckAsync(Scope, null, new SampleRequest("a"), CancellationToken.None);
            IdempotencyCheck other = await _guard.CheckAsync(Scope, "  ", new SampleRequest("b"), CancellationToken.None);

            first.KeySource.ShouldBe(IdempotencyKeySource.Fingerprint);
            first.Key.ShouldStartWith("fp:");
            first.Lifetime.ShouldBe(TimeSpan.FromMinutes(10));
            other.Key.ShouldNotBe(first.Key);
        }

        [Fact]
        public async Task Check_RememberedSubmission_IsReplayed()
        {
            Guid resourceId = Guid.CreateVersion7();
            await RememberAsync("key-1", new SampleRequest("a"), resourceId);

            IdempotencyCheck repeat = await _guard.CheckAsync(Scope, "key-1", new SampleRequest("a"), CancellationToken.None);

            repeat.ReplayOf.ShouldBe(resourceId);
            repeat.CanProceed.ShouldBeFalse();
        }

        [Fact]
        public async Task Check_SameKeyDifferentBody_IsRejected()
        {
            await RememberAsync("key-1", new SampleRequest("a"), Guid.CreateVersion7());

            IdempotencyCheck repeat = await _guard.CheckAsync(Scope, "key-1", new SampleRequest("changed"), CancellationToken.None);

            repeat.Rejection.ShouldNotBeNull().Code.ShouldBe("Idempotency.KeyReused");
        }

        [Fact]
        public async Task Check_SameKeyInAnotherScope_IsIndependent()
        {
            await RememberAsync("key-1", new SampleRequest("a"), Guid.CreateVersion7());

            IdempotencyCheck otherAdviser = await _guard.CheckAsync("ADV-OTHER", "key-1", new SampleRequest("a"), CancellationToken.None);

            otherAdviser.CanProceed.ShouldBeTrue();
        }

        [Fact]
        public async Task Check_KeyTooLong_IsRejected()
        {
            IdempotencyCheck check = await _guard.CheckAsync(Scope, new string('k', 101), new SampleRequest("a"), CancellationToken.None);

            check.Rejection.ShouldNotBeNull().Code.ShouldBe("Idempotency.KeyTooLong");
        }

        [Fact]
        public async Task ExpiredKey_CanProceed_AndIsRenewedInPlace()
        {
            await RememberAsync("key-1", new SampleRequest("a"), Guid.CreateVersion7());
            _time.Advance(TimeSpan.FromHours(25));
            Guid newResource = Guid.CreateVersion7();

            IdempotencyCheck check = await _guard.CheckAsync(Scope, "key-1", new SampleRequest("b"), CancellationToken.None);
            _guard.Remember(check, newResource);
            await _store.SaveChangesAsync(CancellationToken.None);

            check.CanProceed.ShouldBeTrue();
            IdempotencyRecord record = _store.IdempotencyRecords.ShouldHaveSingleItem();
            record.QuoteId.ShouldBe(newResource);
            record.ExpiresAt.ShouldBe(_time.GetUtcNow().AddHours(24));
        }

        [Fact]
        public async Task RecheckAfterConflict_ReplaysTheWinner_OrReturnsNullWhenThereIsNone()
        {
            IdempotencyCheck check = await _guard.CheckAsync(Scope, "key-1", new SampleRequest("a"), CancellationToken.None);

            (await _guard.RecheckAfterConflictAsync(check, CancellationToken.None)).ShouldBeNull();

            Guid winner = Guid.CreateVersion7();
            await RememberAsync("key-1", new SampleRequest("a"), winner);

            IdempotencyCheck? decision = await _guard.RecheckAfterConflictAsync(check, CancellationToken.None);
            decision.ShouldNotBeNull().ReplayOf.ShouldBe(winner);
        }

        private async Task RememberAsync(string key, SampleRequest request, Guid resourceId)
        {
            IdempotencyCheck check = await _guard.CheckAsync(Scope, key, request, CancellationToken.None);
            _guard.Remember(check, resourceId);
            await _store.SaveChangesAsync(CancellationToken.None);
        }

        private sealed record SampleRequest(string Value);
    }
}
