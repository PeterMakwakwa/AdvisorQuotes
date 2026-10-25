using System.Text.Json.Serialization;
using Asp.Versioning;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Quotes.Api.Constants;
using Scalar.AspNetCore;

namespace Quotes.Api.Extensions
{
    /// <summary>
    /// Controllers, URL-segment API versioning (/api/v1/...) and one OpenAPI document per version.
    /// </summary>
    public static class ApiExtensions
    {
        private const string OpenApiEnabledKey = "OpenApi:Enabled";

        private const string ApiTitle = "Advisor Quotes API";

        private const string ApiDescription =
            "Capture, price, view and progress insurance quotes. Errors are RFC 9457 problem details with a stable `code`; "
            + "every response carries `X-Trace-Id` for following the request in traces and logs.";

        private const string IdempotencyKeyDescription =
            "Optional key that makes the request safe to retry: a repeat with the same key and body returns the original quote "
            + "(header Idempotent-Replayed: true); the same key with a different body is rejected with 422. "
            + "Without a key, an identical body from the same adviser within a short window is treated as a duplicate.";

        public static IServiceCollection AddQuotesApi(this IServiceCollection services)
        {
            services.AddControllers()
                .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

            services.AddApiVersioning(options =>
                {
                    options.DefaultApiVersion = new ApiVersion(1, 0);
                    options.ReportApiVersions = true;
                    options.ApiVersionReader = new UrlSegmentApiVersionReader();
                })
                .AddMvc()
                .AddApiExplorer(options =>
                {
                    options.GroupNameFormat = "'v'VVV";
                    options.SubstituteApiVersionInUrl = true;
                })
                .AddOpenApi(options => ConfigureDocument(options.Document));

            return services;
        }

        /// <summary>
        /// The OpenAPI documents and the Scalar reference UI: always in Development, elsewhere when OpenApi:Enabled is true.
        /// </summary>
        public static WebApplication MapQuotesApiReference(this WebApplication app)
        {
            if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>(OpenApiEnabledKey))
            {
                app.MapOpenApi().WithDocumentPerVersion();
                app.MapScalarApiReference(options => options.WithTitle(ApiTitle));
            }

            return app;
        }

        private static void ConfigureDocument(OpenApiOptions options)
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info.Title = ApiTitle;
                document.Info.Description = ApiDescription;

                return Task.CompletedTask;
            });

            options.AddOperationTransformer((operation, _, _) =>
            {
                foreach (IOpenApiParameter parameter in operation.Parameters ?? [])
                {
                    if (parameter is OpenApiParameter header && header.Name == HttpHeaderNames.IdempotencyKey)
                    {
                        header.Description = IdempotencyKeyDescription;
                    }
                }

                return Task.CompletedTask;
            });
        }
    }
}
