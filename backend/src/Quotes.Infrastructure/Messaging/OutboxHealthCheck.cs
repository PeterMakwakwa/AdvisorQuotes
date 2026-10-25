using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Quotes.Domain.Observability.Messages;
using Quotes.Infrastructure.Constants;

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
                [HealthCheckDataKeys.Pending] = backlog.Pending,
                [HealthCheckDataKeys.DeadLettered] = backlog.DeadLettered,
                [HealthCheckDataKeys.OldestPendingAgeSeconds] = Math.Round(backlog.OldestPendingAge.TotalSeconds, 1)
            };

            if (backlog.DeadLettered > 0)
            {
                return HealthCheckResult.Degraded(
                    MessageFormatter.Format(WarningMessages.OutboxDeadLettered, backlog.DeadLettered, _options.MaxAttempts),
                    data: data);
            }

            if (backlog.OldestPendingAge > _options.BacklogAgeWarning)
            {
                return HealthCheckResult.Degraded(
                    MessageFormatter.Format(WarningMessages.OutboxBacklogAged, backlog.OldestPendingAge.TotalSeconds),
                    data: data);
            }

            return HealthCheckResult.Healthy(
                MessageFormatter.Format(InfoMessages.OutboxPending, backlog.Pending),
                data);
        }
    }
}
