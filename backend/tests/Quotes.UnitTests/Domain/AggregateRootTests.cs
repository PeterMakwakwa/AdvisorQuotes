using System.Reflection;
using System.Runtime.CompilerServices;
using Quotes.Domain.Commands;
using Quotes.Domain.Common;
using Quotes.Domain.Entities;
using Quotes.Domain.Enums;
using Quotes.Domain.Repositories;
using Quotes.Domain.ValueObjects;
using Quotes.UnitTests.TestData;
using Shouldly;

namespace Quotes.UnitTests.Domain
{
    /// <summary>
    /// Guards the aggregate root pattern: Quote is the only way in. Nothing outside it can create or edit a
    /// StatusChange, set a property, or modify the history, and only the root has a repository.
    /// </summary>
    public class AggregateRootTests
    {
        private const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;

        private static readonly DateTimeOffset Now = new DateTimeOffset(2026, 10, 24, 9, 0, 0, TimeSpan.Zero);

        [Fact]
        public void Quote_CanOnlyBeCreatedThroughItsFactory()
        {
            typeof(Quote).GetConstructors(PublicInstance).ShouldBeEmpty();
            typeof(Quote).GetMethod(nameof(Quote.Create), BindingFlags.Public | BindingFlags.Static).ShouldNotBeNull();
        }

        [Fact]
        public void Quote_ExposesNoPublicSetters()
        {
            PropertiesWithPublicSetters(typeof(Quote)).ShouldBeEmpty();
        }

        [Fact]
        public void Quote_OnlyStatusAndUpdatedAtCanChangeAfterCreation()
        {
            List<string> mutable = typeof(Quote).GetProperties(PublicInstance)
                .Where(property => property.SetMethod is not null && !IsInitOnly(property))
                .Select(property => property.Name)
                .OrderBy(name => name)
                .ToList();

            mutable.ShouldBe([nameof(Quote.Status), nameof(Quote.UpdatedAt)]);
        }

        [Fact]
        public void Quote_StatusHistory_CannotBeModifiedFromOutside()
        {
            Quote quote = TestQuotes.Draft(Now);

            typeof(Quote).GetProperty(nameof(Quote.StatusHistory))!.PropertyType
                .ShouldBe(typeof(IReadOnlyCollection<StatusChange>));

            ICollection<StatusChange> history = (ICollection<StatusChange>)quote.StatusHistory;
            history.IsReadOnly.ShouldBeTrue();
            Should.Throw<NotSupportedException>(() => history.Clear());
        }

        [Fact]
        public void Quote_StatusAndHistoryOnlyChangeTogether_ThroughChangeStatus()
        {
            Quote quote = TestQuotes.Draft(Now);

            quote.ChangeStatus(new ChangeQuoteStatus { NewStatus = QuoteStatus.SentToClient, ChangedBy = "ADV-001" }, Now);

            quote.Status.ShouldBe(QuoteStatus.SentToClient);
            quote.StatusHistory.Last().ToStatus.ShouldBe(quote.Status);
            quote.StatusHistory.ShouldAllBe(change => change.QuoteId == quote.Id);
        }

        [Fact]
        public void Quote_IsTheAggregateRoot_AndItsEventsCannotBeAddedFromOutside()
        {
            typeof(Quote).IsSubclassOf(typeof(AggregateRoot)).ShouldBeTrue();
            typeof(StatusChange).IsSubclassOf(typeof(AggregateRoot)).ShouldBeFalse();

            Quote quote = TestQuotes.Draft(Now);
            ICollection<IDomainEvent> events = (ICollection<IDomainEvent>)quote.DomainEvents;

            events.IsReadOnly.ShouldBeTrue();
            typeof(AggregateRoot).GetMethod("Raise", BindingFlags.NonPublic | BindingFlags.Instance)!.IsFamily.ShouldBeTrue();
        }

        [Fact]
        public void StatusChange_CanOnlyBeCreatedInsideTheDomain()
        {
            ConstructorInfo[] constructors = typeof(StatusChange).GetConstructors(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            constructors.ShouldNotBeEmpty();
            constructors.ShouldAllBe(constructor => constructor.IsPrivate || constructor.IsAssembly);
        }

        [Fact]
        public void StatusChange_IsImmutableOnceWritten()
        {
            PropertiesWithPublicSetters(typeof(StatusChange)).ShouldBeEmpty();
            typeof(StatusChange).GetProperties(PublicInstance).ShouldAllBe(property => IsInitOnly(property));
        }

        [Fact]
        public void Money_CanOnlyBeCreatedThroughOf()
        {
            typeof(Money).GetConstructors(PublicInstance).ShouldBeEmpty();
            PropertiesWithPublicSetters(typeof(Money)).ShouldBeEmpty();
        }

        [Fact]
        public void Repositories_ExistOnlyForTheAggregateRoot()
        {
            IEnumerable<Type> signatureTypes = typeof(IQuoteRepository).Assembly.GetTypes()
                .Where(type => type.IsInterface && type.Namespace == typeof(IQuoteRepository).Namespace)
                .SelectMany(type => type.GetMethods())
                .SelectMany(method => method.GetParameters().Select(parameter => parameter.ParameterType).Append(method.ReturnType))
                .SelectMany(Unwrap);

            signatureTypes.ShouldNotContain(typeof(StatusChange));
            signatureTypes.ShouldContain(typeof(Quote));
        }

        private static List<string> PropertiesWithPublicSetters(Type type)
        {
            return type.GetProperties(PublicInstance)
                .Where(property => property.SetMethod is { IsPublic: true })
                .Select(property => property.Name)
                .ToList();
        }

        private static bool IsInitOnly(PropertyInfo property)
        {
            return property.SetMethod is not null
                && property.SetMethod.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(IsExternalInit));
        }

        private static IEnumerable<Type> Unwrap(Type type)
        {
            yield return type;

            foreach (Type argument in type.GetGenericArguments().SelectMany(Unwrap))
            {
                yield return argument;
            }
        }
    }
}
