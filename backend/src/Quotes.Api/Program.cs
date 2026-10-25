using System.Globalization;
using Quotes.Api.ErrorHandling;
using Quotes.Api.Extensions;
using Quotes.Api.Logging;
using Quotes.Api.Networking;
using Quotes.Api.RateLimiting;
using Quotes.Application.Extensions;
using Quotes.Application.Idempotency;
using Quotes.Infrastructure.Extensions;
using Serilog;

// A plain console logger until the host is built, so startup failures are still logged.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateLogger();

try
{
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    builder.Services.AddQuotesLogging(builder.Configuration, builder.Environment);

    builder.Services.AddQuotesApi();
    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddQuotesRateLimiting();
    builder.Services.AddQuotesForwardedHeaders();

    builder.Services.AddOptions<IdempotencyOptions>()
        .BindConfiguration(IdempotencyOptions.SectionName)
        .ValidateDataAnnotations()
        .ValidateOnStart();

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure();
    builder.Services.AddHealthChecks()
        .AddDatabaseHealthCheck()
        .AddOutboxHealthCheck();
    builder.Services.AddQuotesTelemetry(builder.Configuration);

    WebApplication app = builder.Build();

    // First, so everything after it (rate limiting, logs, traces) sees the real client address.
    app.UseForwardedHeaders();
    app.UseExceptionHandler();
    app.UseStatusCodePages();
    app.UseTraceIdResponseHeader();
    app.UseSerilogRequestLogging();
    app.UseRateLimiter();

    app.MapQuotesApiReference();
    app.MapControllers();
    app.MapQuotesHealthChecks();
    app.MapPrometheusMetrics();

    app.Run();
}
catch (Exception exception) when (exception is not HostAbortedException)
{
    Log.Fatal(exception, "Advisor Quotes API terminated unexpectedly");

    throw;
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>
/// Exposed for WebApplicationFactory in the integration tests.
/// </summary>
public partial class Program
{
}
