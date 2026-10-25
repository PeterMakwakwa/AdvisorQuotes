using Quotes.Domain.ValueObjects;
using Shouldly;

namespace Quotes.UnitTests.Domain
{
    public class ClientDetailsTests
    {
        [Theory]
        [InlineData("2026-10-23", 34)]
        [InlineData("2026-10-24", 35)]
        [InlineData("2026-10-25", 35)]
        public void AgeOn_CountsCompletedYearsOnly(string asOf, int expectedAge)
        {
            ClientDetails client = new ClientDetails
            {
                FullName = "Thandi Nkosi",
                DateOfBirth = new DateOnly(1991, 10, 24),
                IsSmoker = false
            };

            int age = client.AgeOn(DateOnly.Parse(asOf, System.Globalization.CultureInfo.InvariantCulture));

            age.ShouldBe(expectedAge);
        }

        [Fact]
        public void AgeOn_LeapDayBirthday_TurnsOlderOnFirstOfMarchInNonLeapYear()
        {
            ClientDetails client = new ClientDetails
            {
                FullName = "Thandi Nkosi",
                DateOfBirth = new DateOnly(2000, 2, 29),
                IsSmoker = false
            };

            client.AgeOn(new DateOnly(2026, 2, 28)).ShouldBe(25);
            client.AgeOn(new DateOnly(2026, 3, 1)).ShouldBe(26);
        }
    }
}
