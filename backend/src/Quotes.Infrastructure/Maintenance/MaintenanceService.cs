using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Quotes.Infrastructure.Maintenance
{
    /// <summary>
    /// Runs DatabaseMaintenance on a fixed interval. Every statement is idempotent, so it is safe on several instances.
    /// </summary>
    internal sealed class MaintenanceService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        private readonly MaintenanceOptions _options;

        private readonly ILogger<MaintenanceService> _logger;

        public MaintenanceService(IServiceScopeFactory scopeFactory, IOptions<MaintenanceOptions> options, ILogger<MaintenanceService> logger)
        {
            _scopeFactory = scopeFactory;
            _options = options.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Enabled)
            {
                _logger.MaintenanceDisabled();

                return;
            }

            using PeriodicTimer timer = new PeriodicTimer(_options.Interval);

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await RunOnceAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Host is stopping.
            }
        }

        private async Task RunOnceAsync(CancellationToken stoppingToken)
        {
            try
            {
                using IServiceScope scope = _scopeFactory.CreateScope();
                await scope.ServiceProvider.GetRequiredService<DatabaseMaintenance>().RunAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.MaintenanceFailed(exception);
            }
        }
    }
}
