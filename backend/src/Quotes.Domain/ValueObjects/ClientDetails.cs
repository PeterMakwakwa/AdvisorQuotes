namespace Quotes.Domain.ValueObjects
{
    public sealed record ClientDetails
    {
        public required string FullName { get; init; }

        public required DateOnly DateOfBirth { get; init; }

        public required bool IsSmoker { get; init; }

        public int AgeOn(DateOnly date)
        {
            int age = date.Year - DateOfBirth.Year;

            if (DateOfBirth > date.AddYears(-age))
            {
                age--;
            }

            return age;
        }
    }
}
