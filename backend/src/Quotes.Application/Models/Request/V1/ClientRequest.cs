using Quotes.Domain.ValueObjects;

namespace Quotes.Application.Models.Request.V1
{
    /// <summary>
    /// The client being quoted.
    /// </summary>
    public sealed record ClientRequest
    {
        /// <summary>
        /// The client's full name.
        /// </summary>
        public required string FullName { get; init; }

        /// <summary>
        /// The client's date of birth (yyyy-MM-dd). Used to work out their age for pricing.
        /// </summary>
        public required DateOnly DateOfBirth { get; init; }

        /// <summary>
        /// Whether the client smokes. Smokers pay a loading on the premium.
        /// </summary>
        public required bool IsSmoker { get; init; }

        public ClientDetails BuildClientDetails()
        {
            return new ClientDetails
            {
                FullName = FullName.Trim(),
                DateOfBirth = DateOfBirth,
                IsSmoker = IsSmoker
            };
        }
    }
}
