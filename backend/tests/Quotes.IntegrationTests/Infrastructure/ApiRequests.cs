using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Quotes.Application.Models.Request.V1;
using Quotes.Domain.Enums;

namespace Quotes.IntegrationTests.Infrastructure
{
    internal static class ApiRequests
    {
        public const string QuotesPath = "api/v1/quotes";

        public static readonly JsonSerializerOptions Json = CreateJsonOptions();

        /// <summary>
        /// Each test uses its own adviser, so tests sharing one database never see each other's quotes.
        /// </summary>
        public static string NewAdviserId()
        {
            return $"ADV-{Guid.NewGuid():N}"[..20];
        }

        public static CreateQuoteRequest Quote(string adviserId, decimal coverAmount = 1_000_000m, string? adviserReference = null)
        {
            return new CreateQuoteRequest
            {
                AdviserId = adviserId,
                AdviserReference = adviserReference,
                Client = new ClientRequest
                {
                    FullName = "Thandi Nkosi",
                    DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-35).AddDays(-1),
                    IsSmoker = false
                },
                Product = new ProductRequest
                {
                    ProductCode = "TERM_LIFE",
                    CoverAmount = coverAmount,
                    TermYears = 20
                },
                Notes = "Integration test"
            };
        }

        public static async Task<HttpResponseMessage> PostQuoteAsync(HttpClient client, CreateQuoteRequest request, string? idempotencyKey)
        {
            using HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, QuotesPath)
            {
                Content = JsonContent.Create(request, options: Json)
            };

            if (idempotencyKey is not null)
            {
                message.Headers.Add("Idempotency-Key", idempotencyKey);
            }

            return await client.SendAsync(message);
        }

        public static Task<HttpResponseMessage> PostStatusAsync(HttpClient client, Guid quoteId, QuoteStatus status, string? reason = null)
        {
            ChangeQuoteStatusRequest request = new ChangeQuoteStatusRequest
            {
                NewStatus = status,
                Reason = reason,
                ChangedBy = "ADV-TEST"
            };

            return client.PostAsJsonAsync($"{QuotesPath}/{quoteId}/status-changes", request, Json);
        }

        private static JsonSerializerOptions CreateJsonOptions()
        {
            JsonSerializerOptions options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            options.Converters.Add(new JsonStringEnumConverter());

            return options;
        }
    }
}
