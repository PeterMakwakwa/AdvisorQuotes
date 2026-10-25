using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Quotes.Infrastructure.Messaging
{
    /// <summary>
    /// Polls the outbox and publishes pending messages. A fully published batch is followed straight away by the next
    /// one, so a backlog drains quickly; an exception is logged and retried on the next tick instead of stopping the host.
    /// </summary>
    internal sealed class OutboxDispatcherService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        private readonly OutboxOptions _options;

        private readonly ILogger<OutboxDispatcherService> _logger;

        public OutboxDispatcherService(
            IServiceScopeFactory scopeFactory,
            IOptions<OutboxOptions> options,
            ILogger<OutboxDispatcherService> logger)
        {
            _scopeFactory = scopeFactory;
            _options = options.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Enabled)
            {
                _logger.DispatcherDisabled();

                return;
            }

            using PeriodicTimer timer = new PeriodicTimer(_options.PollingInterval);

            try
            {
                do
                {
                    await DispatchUntilDrainedAsync(stoppingToken);
                }
                while (await timer.WaitForNextTickAsync(stoppingToken));
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Host is stopping.
            }
        }

        private async Task DispatchUntilDrainedAsync(CancellationToken stoppingToken)
        {
            try
            {
                OutboxDispatchResult result;

                // Keep going only while whole batches are being published; failures wait for their backoff instead.
                do
                {
                    using IServiceScope scope = _scopeFactory.CreateScope();
                    result = await scope.ServiceProvider.GetRequiredService<OutboxDispatcher>().DispatchPendingAsync(stoppingToken);
                }
                while (result.Published == _options.BatchSize && !stoppingToken.IsCancellationRequested);

                // Refresh the backlog gauges once per run, so metrics show a growing backlog without querying on scrape.
                using IServiceScope backlogScope = _scopeFactory.CreateScope();
                OutboxBacklog backlog = await backlogScope.ServiceProvider.GetRequiredService<OutboxBacklogReader>().ReadAsync(stoppingToken);
                backlogScope.ServiceProvider.GetRequiredService<MessagingTelemetry>().UpdateBacklog(backlog);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.DispatchFailed(exception);
            }
        }
    }
}
