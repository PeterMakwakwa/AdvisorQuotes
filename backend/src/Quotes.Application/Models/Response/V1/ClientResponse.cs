using Quotes.Domain.ValueObjects;

namespace Quotes.Application.Models.Response.V1
{
    public sealed record ClientResponse
    {
        public required string FullName { get; init; }

        public required DateOnly DateOfBirth { get; init; }

        public required bool IsSmoker { get; init; }

        public static ClientResponse FromDomain(ClientDetails client)
        {
            return new ClientResponse
            {
                FullName = client.FullName,
                DateOfBirth = client.DateOfBirth,
                IsSmoker = client.IsSmoker
            };
        }
    }
}
