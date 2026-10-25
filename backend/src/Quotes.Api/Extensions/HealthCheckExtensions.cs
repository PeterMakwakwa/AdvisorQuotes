using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Quotes.Infrastructure.Extensions;

namespace Quotes.Api.Extensions
{
    public static class HealthCheckExtensions
    {
        private const string HealthPath = "/health";

        private const string LivePath = "/health/live";

        private const string ReadyPath = "/health/ready";

        /// <summary>
        /// /health/live: the process is running (no dependencies checked).
        /// /health/ready: the database is reachable and migrated, so the instance can take traffic.
        /// /health: every check, including the outbox backlog, for operators and dashboards (Degraded still returns 200).
        /// </summary>
        public static IEndpointRouteBuilder MapQuotesHealthChecks(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapHealthChecks(LivePath, new HealthCheckOptions
            {
                Predicate = _ => false,
                ResponseWriter = WriteResponseAsync
            });

            endpoints.MapHealthChecks(ReadyPath, new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains(ServiceCollectionExtensions.ReadinessTag),
                ResponseWriter = WriteResponseAsync
            });

            endpoints.MapHealthChecks(HealthPath, new HealthCheckOptions
            {
                ResponseWriter = WriteResponseAsync
            });

            return endpoints;
        }

        private static Task WriteResponseAsync(HttpContext context, HealthReport report)
        {
            HealthResponse response = new HealthResponse
            {
                Status = report.Status.ToString(),
                DurationMs = report.TotalDuration.TotalMilliseconds,
                Checks = report.Entries
                    .Select(entry => new HealthCheckResponse
                    {
                        Name = entry.Key,
                        Status = entry.Value.Status.ToString(),
                        Description = entry.Value.Description,
                        DurationMs = entry.Value.Duration.TotalMilliseconds,
                        Data = entry.Value.Data.Count == 0 ? null : entry.Value.Data
                    })
                    .ToList()
            };

            return context.Response.WriteAsJsonAsync(response);
        }

        private sealed record HealthResponse
        {
            public required string Status { get; init; }

            public required double DurationMs { get; init; }

            public required IReadOnlyList<HealthCheckResponse> Checks { get; init; }
        }

        private sealed record HealthCheckResponse
        {
            public required string Name { get; init; }

            public required string Status { get; init; }

            public string? Description { get; init; }

            public required double DurationMs { get; init; }

            public IReadOnlyDictionary<string, object>? Data { get; init; }
        }
    }
}
