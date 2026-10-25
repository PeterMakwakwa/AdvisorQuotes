using System.Globalization;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Quotes.Infrastructure.Messaging
{
    /// <summary>
    /// Degraded (not Unhealthy) when events are stuck: a broker problem must raise an alarm, but it is no reason to take
    /// instances out of the load balancer, because quotes can still be captured and the events will be published later.
    /// Not part of readiness.
    /// </summary>
    internal sealed class OutboxHealthCheck : IHealthCheck
    {
        private readonly OutboxBacklogReader _reader;

        private readonly MessagingTelemetry _telemetry;

        private readonly OutboxOptions _options;

        public OutboxHealthCheck(OutboxBacklogReader reader, MessagingTelemetry telemetry, IOptions<OutboxOptions> options)
        {
            _reader = reader;
            _telemetry = telemetry;
            _options = options.Value;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
        {
            OutboxBacklog backlog = await _reader.ReadAsync(cancellationToken);
            _telemetry.UpdateBacklog(backlog);

            Dictionary<string, object> data = new Dictionary<string, object>
            {
                ["pending"] = backlog.Pending,
                ["deadLettered"] = backlog.DeadLettered,
                ["oldestPendingAgeSeconds"] = Math.Round(backlog.OldestPendingAge.TotalSeconds, 1)
            };

            if (backlog.DeadLettered > 0)
            {
                return HealthCheckResult.Degraded(
                    string.Create(CultureInfo.InvariantCulture, $"{backlog.DeadLettered} outbox message(s) gave up after {_options.MaxAttempts} attempts."),
                    data: data);
            }

            if (backlog.OldestPendingAge > _options.BacklogAgeWarning)
            {
                return HealthCheckResult.Degraded(
                    string.Create(CultureInfo.InvariantCulture, $"The oldest pending outbox message has waited {backlog.OldestPendingAge.TotalSeconds:N0} s."),
                    data: data);
            }

            return HealthCheckResult.Healthy(
                string.Create(CultureInfo.InvariantCulture, $"{backlog.Pending} outbox message(s) pending."),
                data);
        }
    }
}
