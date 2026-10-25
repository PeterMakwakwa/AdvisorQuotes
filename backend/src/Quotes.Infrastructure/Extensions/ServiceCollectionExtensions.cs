using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Quotes.Application.Interfaces;
using Quotes.Domain.Observability.Messages;
using Quotes.Domain.Repositories;
using Quotes.Infrastructure.Constants;
using Quotes.Infrastructure.Maintenance;
using Quotes.Infrastructure.Messaging;
using Quotes.Infrastructure.Persistence;
using Quotes.Infrastructure.Persistence.Repositories;
using Quotes.Infrastructure.Pricing;

namespace Quotes.Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        // Proves the database is reachable and the migrations have run, not just that the server is up.
        private const string DatabaseHealthQuery = "SELECT 1 FROM quotes LIMIT 1;";

        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            // Timestamps at the database's precision, so values returned after a save equal the stored ones.
            services.Replace(ServiceDescriptor.Singleton<TimeProvider>(new MicrosecondPrecisionTimeProvider(TimeProvider.System)));

            services.AddOptions<DatabaseOptions>()
                .BindConfiguration(DatabaseOptions.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddDbContext<QuotesDbContext>((serviceProvider, options) =>
            {
                DatabaseOptions database = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;

                options.UseNpgsql(
                    GetConnectionString(serviceProvider),
                    npgsql => npgsql.EnableRetryOnFailure(database.MaxRetryCount, database.MaxRetryDelay, errorCodesToAdd: null));
            });

            services.AddScoped<IQuoteRepository, QuoteRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IIdempotencyStore, IdempotencyStore>();
            services.AddScoped<IQuoteReadService, QuoteReadService>();
            services.AddScoped<IQuoteReferenceGenerator, QuoteReferenceGenerator>();

            services.AddOptions<PricingOptions>()
                .BindConfiguration(PricingOptions.SectionName)
                .ValidateOnStart();
            services.AddSingleton<IValidateOptions<PricingOptions>, PricingOptionsValidator>();
            services.AddSingleton<IRateTableProvider, ConfigurationRateTableProvider>();

            services.AddOptions<OutboxOptions>()
                .BindConfiguration(OutboxOptions.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();
            services.AddSingleton<MessagingTelemetry>();
            services.AddSingleton<IIntegrationEventPublisher, LoggingIntegrationEventPublisher>();
            services.AddScoped<OutboxDispatcher>();
            services.AddScoped<OutboxBacklogReader>();
            services.AddHostedService<OutboxDispatcherService>();

            services.AddOptions<MaintenanceOptions>()
                .BindConfiguration(MaintenanceOptions.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();
            services.AddScoped<DatabaseMaintenance>();
            services.AddHostedService<MaintenanceService>();

            return services;
        }

        /// <summary>
        /// Reports Degraded when outbox messages are stuck. Deliberately not tagged for readiness.
        /// </summary>
        public static IHealthChecksBuilder AddOutboxHealthCheck(this IHealthChecksBuilder builder)
        {
            return builder.AddCheck<OutboxHealthCheck>(HealthCheckNames.Outbox, tags: [HealthCheckTags.Outbox]);
        }

        public static IHealthChecksBuilder AddDatabaseHealthCheck(this IHealthChecksBuilder builder)
        {
            return builder.AddNpgSql(
                GetConnectionString,
                healthQuery: DatabaseHealthQuery,
                name: HealthCheckNames.Database,
                tags: [HealthCheckTags.Readiness]);
        }

        private static string GetConnectionString(IServiceProvider serviceProvider)
        {
            IConfiguration configuration = serviceProvider.GetRequiredService<IConfiguration>();

            return configuration.GetConnectionString(ConnectionStringNames.QuotesDb)
                ?? throw new InvalidOperationException(
                    MessageFormatter.Format(ErrorMessages.ConnectionStringMissing, ConnectionStringNames.QuotesDb));
        }
    }
}
