using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Quotes.Application.Abstractions;
using Quotes.Application.Handlers;
using Quotes.Application.Idempotency;
using Quotes.Application.Interfaces;
using Quotes.Application.Models.Commands;
using Quotes.Application.Models.Queries;
using Quotes.Application.Models.Response.V1;
using Quotes.Application.Models.Results;
using Quotes.Application.Observability;
using Quotes.Application.Pricing;
using Quotes.Application.Validators;

namespace Quotes.Application.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers handlers, validators, metrics and in-process pricing. Options are bound by the host (Api) from configuration.
        /// </summary>
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.TryAddSingleton(TimeProvider.System);

            services.AddMetrics();
            services.AddSingleton<QuotesMetrics>();

            services.AddValidatorsFromAssemblyContaining<CreateQuoteRequestValidator>();

            services.AddScoped<IPricingService, LocalPricingService>();

            services.AddScoped<IIdempotencyGuard, IdempotencyGuard>();

            // Command handlers hold only business logic; the instrumented decorators add spans, metrics and logs around them.
            services.AddScoped<CreateQuoteHandler>();
            services.AddScoped<ICommandHandler<CreateQuoteCommand, CreateQuoteResult>>(provider => new InstrumentedCreateQuoteHandler(
                provider.GetRequiredService<CreateQuoteHandler>(),
                provider.GetRequiredService<QuotesMetrics>(),
                provider.GetRequiredService<ILogger<InstrumentedCreateQuoteHandler>>()));

            services.AddScoped<ChangeQuoteStatusHandler>();
            services.AddScoped<ICommandHandler<ChangeQuoteStatusCommand, QuoteResponse>>(provider => new InstrumentedChangeQuoteStatusHandler(
                provider.GetRequiredService<ChangeQuoteStatusHandler>(),
                provider.GetRequiredService<QuotesMetrics>(),
                provider.GetRequiredService<ILogger<InstrumentedChangeQuoteStatusHandler>>()));

            services.AddScoped<IQueryHandler<GetQuoteQuery, QuoteResponse>, GetQuoteHandler>();
            services.AddScoped<IQueryHandler<ListQuotesQuery, PagedResponse<QuoteSummaryResponse>>, ListQuotesHandler>();

            return services;
        }
    }
}
