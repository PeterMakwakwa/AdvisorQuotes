using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Quotes.Application.Interfaces;
using Quotes.Domain.Repositories;
using Quotes.Infrastructure.Maintenance;
using Quotes.Infrastructure.Messaging;
using Quotes.Infrastructure.Persistence;
using Quotes.Infrastructure.Persistence.Repositories;
using Quotes.Infrastructure.Pricing;

namespace Quotes.Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// The connection string name. Locally it comes from appsettings.Development.json; elsewhere set the
        /// environment variable ConnectionStrings__QuotesDb.
        /// </summary>
        public const string ConnectionStringName = "QuotesDb";

        public const string ReadinessTag = "ready";

        public const string OutboxTag = "outbox";

        private const string DatabaseHealthCheckName = "postgres";

        private const string OutboxHealthCheckName = "outbox";

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
            return builder.AddCheck<OutboxHealthCheck>(OutboxHealthCheckName, tags: [OutboxTag]);
        }

        public static IHealthChecksBuilder AddDatabaseHealthCheck(this IHealthChecksBuilder builder)
        {
            return builder.AddNpgSql(
                GetConnectionString,
                healthQuery: DatabaseHealthQuery,
                name: DatabaseHealthCheckName,
                tags: [ReadinessTag]);
        }

        private static string GetConnectionString(IServiceProvider serviceProvider)
        {
            IConfiguration configuration = serviceProvider.GetRequiredService<IConfiguration>();

            return configuration.GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException(
                    $"Connection string '{ConnectionStringName}' is not configured. Set ConnectionStrings__{ConnectionStringName}.");
        }
    }
}
