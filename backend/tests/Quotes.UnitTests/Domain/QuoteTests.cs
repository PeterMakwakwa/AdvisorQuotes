using Quotes.Domain.Commands;
using Quotes.Domain.Common;
using Quotes.Domain.Entities;
using Quotes.Domain.Enums;
using Quotes.Domain.Events;
using Quotes.Domain.Rules;
using Quotes.Domain.ValueObjects;
using Shouldly;

namespace Quotes.UnitTests.Domain
{
    public class QuoteTests
    {
        private const string AdviserId = "ADV-001";

        private static readonly DateTimeOffset Now = new DateTimeOffset(2026, 10, 24, 9, 0, 0, TimeSpan.Zero);

        [Fact]
        public void OpenStatuses_AreTheStatusesThatCanStillChange()
        {
            QuoteStatusTransitions.OpenStatuses.ShouldBe([QuoteStatus.Draft, QuoteStatus.SentToClient]);
        }

        [Fact]
        public void Create_WithValidInput_StartsAsDraftWithInitialHistoryEntry()
        {
            Quote quote = CreateDraft();

            quote.Status.ShouldBe(QuoteStatus.Draft);
            StatusChange entry = quote.StatusHistory.ShouldHaveSingleItem();
            entry.FromStatus.ShouldBeNull();
            entry.ToStatus.ShouldBe(QuoteStatus.Draft);
            entry.ChangedBy.ShouldBe(AdviserId);
        }

        [Fact]
        public void Create_WithDateOfBirthInFuture_Fails()
        {
            CreateQuote command = BuildCreateCommand() with
            {
                Client = BuildCreateCommand().Client with { DateOfBirth = new DateOnly(2030, 1, 1) }
            };

            Result<Quote> result = Quote.Create(command, Now);

            result.IsFailure.ShouldBeTrue();
            result.Error!.Code.ShouldBe("Quote.DateOfBirthNotInPast");
        }

        [Fact]
        public void Create_RaisesQuoteCreatedWithTheQuoteDetails()
        {
            Quote quote = CreateDraft();

            QuoteCreated created = quote.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<QuoteCreated>();
            created.QuoteId.ShouldBe(quote.Id);
            created.ExternalReference.ShouldBe("Q-2026-000001");
            created.ProductCode.ShouldBe("TERM_LIFE");
            created.MonthlyPremium.ShouldBe(450.00m);
            created.OccurredAt.ShouldBe(Now);
            created.EventId.ShouldNotBe(Guid.Empty);
        }

        [Fact]
        public void ChangeStatus_RaisesQuoteStatusChangedWithFromAndTo()
        {
            Quote quote = CreateDraft();
            quote.ClearDomainEvents();

            quote.ChangeStatus(StatusCommand(QuoteStatus.Declined, "Too expensive"), Now.AddMinutes(1));

            QuoteStatusChanged changed = quote.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<QuoteStatusChanged>();
            changed.QuoteId.ShouldBe(quote.Id);
            changed.FromStatus.ShouldBe(QuoteStatus.Draft);
            changed.ToStatus.ShouldBe(QuoteStatus.Declined);
            changed.Reason.ShouldBe("Too expensive");
            changed.ChangedBy.ShouldBe(AdviserId);
        }

        [Fact]
        public void ChangeStatus_Rejected_RaisesNoEvent()
        {
            Quote quote = CreateDraft();
            quote.ClearDomainEvents();

            quote.ChangeStatus(StatusCommand(QuoteStatus.Accepted), Now);

            quote.DomainEvents.ShouldBeEmpty();
        }

        [Theory]
        [InlineData("  CRM-0042  ", "CRM-0042")]
        [InlineData("   ", null)]
        [InlineData(null, null)]
        public void Create_AdviserReference_IsTrimmedOrLeftEmpty(string? supplied, string? expected)
        {
            CreateQuote command = BuildCreateCommand() with { AdviserReference = supplied };

            Quote quote = Quote.Create(command, Now).Value;

            quote.AdviserReference.ShouldBe(expected);
        }

        [Theory]
        [InlineData(QuoteStatus.SentToClient)]
        [InlineData(QuoteStatus.Declined)]
        public void ChangeStatus_FromDraftToAllowedStatus_SucceedsAndRecordsHistory(QuoteStatus target)
        {
            Quote quote = CreateDraft();

            Result result = quote.ChangeStatus(StatusCommand(target, "Reason given"), Now.AddMinutes(5));

            result.IsSuccess.ShouldBeTrue();
            quote.Status.ShouldBe(target);
            quote.StatusHistory.Count.ShouldBe(2);
            quote.UpdatedAt.ShouldBe(Now.AddMinutes(5));
        }

        [Fact]
        public void ChangeStatus_FromDraftToAccepted_IsRejected()
        {
            Quote quote = CreateDraft();

            Result result = quote.ChangeStatus(StatusCommand(QuoteStatus.Accepted), Now);

            result.Error!.Code.ShouldBe("Quote.InvalidStatusTransition");
            quote.Status.ShouldBe(QuoteStatus.Draft);
            quote.StatusHistory.Count.ShouldBe(1);
        }

        [Fact]
        public void ChangeStatus_FromTerminalStatus_IsRejected()
        {
            Quote quote = CreateDraft();
            quote.ChangeStatus(StatusCommand(QuoteStatus.SentToClient), Now);
            quote.ChangeStatus(StatusCommand(QuoteStatus.Accepted), Now);

            Result result = quote.ChangeStatus(StatusCommand(QuoteStatus.Declined, "Changed mind"), Now);

            result.Error!.Code.ShouldBe("Quote.InvalidStatusTransition");
            quote.Status.ShouldBe(QuoteStatus.Accepted);
        }

        [Fact]
        public void ChangeStatus_ToSameStatus_IsRejected()
        {
            Quote quote = CreateDraft();

            Result result = quote.ChangeStatus(StatusCommand(QuoteStatus.Draft), Now);

            result.Error!.Code.ShouldBe("Quote.StatusUnchanged");
        }

        [Fact]
        public void ChangeStatus_ToDeclinedWithoutReason_IsRejected()
        {
            Quote quote = CreateDraft();

            Result result = quote.ChangeStatus(StatusCommand(QuoteStatus.Declined, "  "), Now);

            result.Error!.Code.ShouldBe("Quote.DeclineReasonRequired");
        }

        private static CreateQuote BuildCreateCommand()
        {
            return new CreateQuote
            {
                ExternalReference = "Q-2026-000001",
                AdviserId = AdviserId,
                Client = new ClientDetails
                {
                    FullName = "Thandi Nkosi",
                    DateOfBirth = new DateOnly(1990, 5, 17),
                    IsSmoker = false
                },
                Product = new ProductDetails
                {
                    ProductCode = "TERM_LIFE",
                    CoverAmount = 1_000_000m,
                    TermYears = 20
                },
                MonthlyPremium = Money.Of(450.00m, "ZAR"),
                PricingVersion = "v1"
            };
        }

        private static Quote CreateDraft()
        {
            return Quote.Create(BuildCreateCommand(), Now).Value;
        }

        private static ChangeQuoteStatus StatusCommand(QuoteStatus status, string? reason = null)
        {
            return new ChangeQuoteStatus { NewStatus = status, Reason = reason, ChangedBy = AdviserId };
        }
    }
}
