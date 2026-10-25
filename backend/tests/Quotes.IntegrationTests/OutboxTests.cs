using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Quotes.Application.Models.Response.V1;
using Quotes.Domain.Enums;
using Quotes.Infrastructure.Maintenance;
using Quotes.Infrastructure.Messaging;
using Quotes.IntegrationTests.Infrastructure;
using Shouldly;

namespace Quotes.IntegrationTests
{
    /// <summary>
    /// Domain events reach the outbox in the same transaction as the change, and the dispatcher publishes each of them
    /// exactly once (per successful attempt), continuing the original request's trace.
    /// </summary>
    [Collection(ApiCollection.Name)]
    public class OutboxTests
    {
        private readonly QuotesApiFactory _factory;

        private readonly HttpClient _client;

        public OutboxTests(QuotesApiFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task CreatingAQuote_WritesQuoteCreatedToTheOutboxWithTheTraceContext()
        {
            QuoteResponse quote = await CreateQuoteAsync();

            OutboxRow row = (await _factory.GetOutboxRowsAsync(quote.Id)).ShouldHaveSingleItem();

            row.EventType.ShouldBe(IntegrationEvents.QuoteCreatedType);
            row.ProcessedAt.ShouldBeNull();
            row.Attempts.ShouldBe(0);
            row.TraceParent.ShouldNotBeNull().ShouldStartWith("00-");
            using JsonDocument payload = JsonDocument.Parse(row.Payload);
            payload.RootElement.GetProperty("externalReference").GetString().ShouldBe(quote.ExternalReference);
            payload.RootElement.GetProperty("monthlyPremium").GetDecimal().ShouldBe(300.00m);
        }

        [Fact]
        public async Task StatusChange_WritesQuoteStatusChangedWithFromAndTo()
        {
            QuoteResponse quote = await CreateQuoteAsync();

            (await ApiRequests.PostStatusAsync(_client, quote.Id, QuoteStatus.SentToClient, "Emailed")).StatusCode.ShouldBe(HttpStatusCode.OK);

            List<OutboxRow> rows = await _factory.GetOutboxRowsAsync(quote.Id);
            rows.Select(row => row.EventType).ShouldBe([IntegrationEvents.QuoteCreatedType, IntegrationEvents.QuoteStatusChangedType]);
            using JsonDocument payload = JsonDocument.Parse(rows[1].Payload);
            payload.RootElement.GetProperty("fromStatus").GetString().ShouldBe("Draft");
            payload.RootElement.GetProperty("toStatus").GetString().ShouldBe("SentToClient");
            payload.RootElement.GetProperty("reason").GetString().ShouldBe("Emailed");
        }

        [Fact]
        public async Task ReplayedOrRejectedRequests_WriteNoExtraEvents()
        {
            string adviserId = ApiRequests.NewAdviserId();
            HttpResponseMessage created = await ApiRequests.PostQuoteAsync(_client, ApiRequests.Quote(adviserId), "outbox-replay");
            QuoteResponse quote = (await created.Content.ReadFromJsonAsync<QuoteResponse>(ApiRequests.Json))!;

            await ApiRequests.PostQuoteAsync(_client, ApiRequests.Quote(adviserId), "outbox-replay");
            await ApiRequests.PostQuoteAsync(_client, ApiRequests.Quote(adviserId, 2_000_000m), "outbox-replay");
            await ApiRequests.PostStatusAsync(_client, quote.Id, QuoteStatus.Accepted);

            (await _factory.GetOutboxRowsAsync(quote.Id)).ShouldHaveSingleItem().EventType.ShouldBe(IntegrationEvents.QuoteCreatedType);
        }

        [Fact]
        public async Task Dispatcher_PublishesEachMessageOnce_InTheOriginalTrace()
        {
            await _factory.DrainOutboxAsync();
            QuoteResponse quote = await CreateQuoteAsync();
            OutboxRow pending = (await _factory.GetOutboxRowsAsync(quote.Id)).ShouldHaveSingleItem();
            ConcurrentBag<Activity> publishSpans = [];
            using ActivityListener listener = ListenToPublishSpans(publishSpans);

            await _factory.DrainOutboxAsync();
            await _factory.DrainOutboxAsync();

            _factory.Publisher.Published.Count(message => message.Id == pending.Id).ShouldBe(1);
            OutboxRow published = (await _factory.GetOutboxRowsAsync(quote.Id)).ShouldHaveSingleItem();
            published.ProcessedAt.ShouldNotBeNull();
            published.Attempts.ShouldBe(1);

            // The publish span continues the trace of the HTTP request that created the quote.
            Activity span = publishSpans.Single(activity => Equals(activity.GetTagItem(MessagingTelemetry.TagNames.MessageId), pending.Id));
            string[] traceParent = pending.TraceParent!.Split('-');
            span.TraceId.ToString().ShouldBe(traceParent[1]);
            span.ParentSpanId.ToString().ShouldBe(traceParent[2]);
            span.Kind.ShouldBe(ActivityKind.Producer);
        }

        [Fact]
        public async Task Dispatcher_FailedPublish_IsRecordedAndRetriedLater()
        {
            await _factory.DrainOutboxAsync();
            QuoteResponse quote = await CreateQuoteAsync();
            _factory.Publisher.FailWhenPayloadContains(quote.Id.ToString());

            try
            {
                await DispatchOnceAsync();

                OutboxRow failed = (await _factory.GetOutboxRowsAsync(quote.Id)).ShouldHaveSingleItem();
                failed.ProcessedAt.ShouldBeNull();
                failed.Attempts.ShouldBe(1);
                failed.LastError.ShouldNotBeNull().ShouldContain("Broker unavailable");
            }
            finally
            {
                _factory.Publisher.FailWhenPayloadContains(null);
            }

            await _factory.DrainOutboxAsync();

            OutboxRow recovered = (await _factory.GetOutboxRowsAsync(quote.Id)).ShouldHaveSingleItem();
            recovered.ProcessedAt.ShouldNotBeNull();
            recovered.Attempts.ShouldBe(2);
            recovered.LastError.ShouldBeNull();
        }

        [Fact]
        public async Task MessagesForOneQuote_ArePublishedInOrder_WhileAnEarlierOneIsRetrying()
        {
            await _factory.DrainOutboxAsync();
            QuoteResponse quote = await CreateQuoteAsync();
            QuoteResponse otherQuote = await CreateQuoteAsync();
            (await ApiRequests.PostStatusAsync(_client, quote.Id, QuoteStatus.SentToClient, "Emailed")).StatusCode.ShouldBe(HttpStatusCode.OK);
            List<OutboxRow> rows = await _factory.GetOutboxRowsAsync(quote.Id);
            Guid createdId = rows[0].Id;
            Guid statusChangedId = rows[1].Id;
            Guid otherCreatedId = (await _factory.GetOutboxRowsAsync(otherQuote.Id)).Single().Id;

            // Only the quote's QuoteCreated fails (its payload carries its own event id).
            _factory.Publisher.FailWhenPayloadContains(createdId.ToString());

            try
            {
                await DispatchOnceAsync();
                await DispatchOnceAsync();

                // The status change waits behind the failing creation; other quotes are not held up.
                _factory.Publisher.Published.ShouldNotContain(message => message.Id == statusChangedId);
                _factory.Publisher.Published.ShouldContain(message => message.Id == otherCreatedId);
                (await _factory.GetOutboxRowsAsync(quote.Id))[1].Attempts.ShouldBe(0);
            }
            finally
            {
                _factory.Publisher.FailWhenPayloadContains(null);
            }

            await _factory.DrainOutboxAsync();

            List<Guid> publishedOrder = _factory.Publisher.Published.Select(message => message.Id).ToList();
            publishedOrder.IndexOf(createdId).ShouldBeLessThan(publishedOrder.IndexOf(statusChangedId));
            (await _factory.GetOutboxRowsAsync(quote.Id)).ShouldAllBe(row => row.ProcessedAt != null);
        }

        [Fact]
        public async Task Dispatchers_RunningInParallel_PublishEachMessageExactlyOnce()
        {
            await _factory.DrainOutboxAsync();
            QuoteResponse[] quotes = await Task.WhenAll(Enumerable.Range(0, 12).Select(_ => CreateQuoteAsync()));
            List<Guid> eventIds = [];

            foreach (QuoteResponse quote in quotes)
            {
                eventIds.Add((await _factory.GetOutboxRowsAsync(quote.Id)).Single().Id);
            }

            await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => DispatchOnceAsync()));
            await _factory.DrainOutboxAsync();

            foreach (Guid eventId in eventIds)
            {
                _factory.Publisher.Published.Count(message => message.Id == eventId).ShouldBe(1);
            }
        }

        [Fact]
        public async Task SlowPublisher_HoldsALeaseButNoDatabaseLock()
        {
            await _factory.DrainOutboxAsync();
            QuoteResponse quote = await CreateQuoteAsync();
            OutboxRow pending = (await _factory.GetOutboxRowsAsync(quote.Id)).ShouldHaveSingleItem();
            PublishGate gate = _factory.Publisher.HoldWhenPayloadContains(quote.Id.ToString());

            Task<OutboxDispatchResult> dispatching = DispatchOnceAsync();

            try
            {
                await gate.WaitUntilHeldAsync();

                // The broker is "slow" right now. The row is claimed (leased) but not locked: another connection can lock
                // it without waiting, so no transaction is held open while publishing.
                (await _factory.TryLockOutboxRowAsync(pending.Id)).ShouldBeTrue();
                OutboxRow claimed = (await _factory.GetOutboxRowsAsync(quote.Id)).ShouldHaveSingleItem();
                claimed.LockedUntil.ShouldNotBeNull().ShouldBeGreaterThan(DateTimeOffset.UtcNow);
                claimed.LockToken.ShouldNotBeNull();
                claimed.ProcessedAt.ShouldBeNull();
            }
            finally
            {
                gate.Release();
            }

            (await dispatching).Published.ShouldBe(1);
            OutboxRow published = (await _factory.GetOutboxRowsAsync(quote.Id)).ShouldHaveSingleItem();
            published.ProcessedAt.ShouldNotBeNull();
            published.LockedUntil.ShouldBeNull();
            published.LockToken.ShouldBeNull();
        }

        [Fact]
        public async Task LeasedMessage_IsSkipped_UntilTheLeaseExpires()
        {
            await _factory.DrainOutboxAsync();
            QuoteResponse quote = await CreateQuoteAsync();
            OutboxRow pending = (await _factory.GetOutboxRowsAsync(quote.Id)).ShouldHaveSingleItem();

            // Another dispatcher holds the lease...
            await _factory.ExecuteSqlAsync(
                "UPDATE outbox_messages SET locked_until = now() + interval '1 hour', lock_token = gen_random_uuid() WHERE id = @id",
                ("id", pending.Id));
            await _factory.DrainOutboxAsync();
            _factory.Publisher.Published.ShouldNotContain(message => message.Id == pending.Id);

            // ...then dies: once its lease has expired, the message is picked up again.
            await _factory.ExecuteSqlAsync(
                "UPDATE outbox_messages SET locked_until = now() - interval '1 second' WHERE id = @id",
                ("id", pending.Id));
            await _factory.DrainOutboxAsync();

            _factory.Publisher.Published.Count(message => message.Id == pending.Id).ShouldBe(1);
            (await _factory.GetOutboxRowsAsync(quote.Id)).ShouldHaveSingleItem().ProcessedAt.ShouldNotBeNull();
        }

        [Fact]
        public async Task ExpiredLease_TakenOver_CannotBeOverwrittenByTheOldHolder()
        {
            await _factory.DrainOutboxAsync();
            QuoteResponse quote = await CreateQuoteAsync();
            OutboxRow pending = (await _factory.GetOutboxRowsAsync(quote.Id)).ShouldHaveSingleItem();
            PublishGate gate = _factory.Publisher.HoldWhenPayloadContains(quote.Id.ToString());
            Guid takeoverToken = Guid.NewGuid();

            Task<OutboxDispatchResult> dispatching = DispatchOnceAsync();
            await gate.WaitUntilHeldAsync();

            // While the first dispatcher is stuck publishing, its lease runs out and a second dispatcher claims the message.
            await _factory.ExecuteSqlAsync(
                "UPDATE outbox_messages SET locked_until = now() + interval '1 hour', lock_token = @token WHERE id = @id",
                ("token", takeoverToken),
                ("id", pending.Id));
            gate.Release();
            OutboxDispatchResult result = await dispatching;

            // The first dispatcher's late "published" is rejected: the second holder's lease and result stand.
            result.Published.ShouldBe(0);
            OutboxRow afterwards = (await _factory.GetOutboxRowsAsync(quote.Id)).ShouldHaveSingleItem();
            afterwards.LockToken.ShouldBe(takeoverToken);
            afterwards.ProcessedAt.ShouldBeNull();
            afterwards.Attempts.ShouldBe(0);

            // Tidy up: let the message be published normally so later tests start from an empty outbox.
            await _factory.ExecuteSqlAsync(
                "UPDATE outbox_messages SET locked_until = NULL, lock_token = NULL WHERE id = @id",
                ("id", pending.Id));
            await _factory.DrainOutboxAsync();
        }

        [Fact]
        public async Task Maintenance_RemovesOnlyExpiredKeysAndOldPublishedMessages()
        {
            string oldAdviser = ApiRequests.NewAdviserId();
            string currentAdviser = ApiRequests.NewAdviserId();
            QuoteResponse oldQuote = await CreateQuoteAsync(oldAdviser);
            QuoteResponse currentQuote = await CreateQuoteAsync(currentAdviser);
            await _factory.DrainOutboxAsync();

            await _factory.ExecuteSqlAsync(
                "UPDATE idempotency_keys SET expires_at = now() - interval '2 days' WHERE adviser_id = @adviserId",
                ("adviserId", oldAdviser));
            await _factory.ExecuteSqlAsync(
                "UPDATE outbox_messages SET processed_at = now() - interval '8 days' WHERE payload->>'quoteId' = @quoteId",
                ("quoteId", oldQuote.Id.ToString()));

            MaintenanceResult result = await _factory.InScopeAsync(services =>
                services.GetRequiredService<DatabaseMaintenance>().RunAsync(CancellationToken.None));

            result.IdempotencyKeysDeleted.ShouldBe(1);
            result.OutboxMessagesDeleted.ShouldBe(1);
            (await _factory.CountRowsAsync("idempotency_keys", oldAdviser)).ShouldBe(0);
            (await _factory.CountRowsAsync("idempotency_keys", currentAdviser)).ShouldBe(1);
            (await _factory.GetOutboxRowsAsync(oldQuote.Id)).ShouldBeEmpty();
            (await _factory.GetOutboxRowsAsync(currentQuote.Id)).ShouldHaveSingleItem();
            (await _factory.CountRowsAsync("quotes", oldAdviser)).ShouldBe(1);
        }

        private async Task<QuoteResponse> CreateQuoteAsync(string? adviserId = null)
        {
            HttpResponseMessage response = await ApiRequests.PostQuoteAsync(
                _client, ApiRequests.Quote(adviserId ?? ApiRequests.NewAdviserId()), Guid.NewGuid().ToString());

            response.StatusCode.ShouldBe(HttpStatusCode.Created);

            return (await response.Content.ReadFromJsonAsync<QuoteResponse>(ApiRequests.Json))!;
        }

        private Task<OutboxDispatchResult> DispatchOnceAsync()
        {
            return _factory.InScopeAsync(services =>
                services.GetRequiredService<OutboxDispatcher>().DispatchPendingAsync(CancellationToken.None));
        }

        private static ActivityListener ListenToPublishSpans(ConcurrentBag<Activity> spans)
        {
            ActivityListener listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == MessagingTelemetry.SourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = spans.Add
            };

            ActivitySource.AddActivityListener(listener);

            return listener;
        }
    }
}
