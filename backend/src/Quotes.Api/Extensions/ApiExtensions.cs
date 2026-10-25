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
                    options.GroupNameFormat = ApiVersions.GroupNameFormat;
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
            if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>(ConfigurationKeys.OpenApiEnabled))
            {
                app.MapOpenApi().WithDocumentPerVersion();
                app.MapScalarApiReference(options => options.WithTitle(OpenApiDocumentation.Title));
            }

            return app;
        }

        private static void ConfigureDocument(OpenApiOptions options)
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info.Title = OpenApiDocumentation.Title;
                document.Info.Description = OpenApiDocumentation.Description;

                return Task.CompletedTask;
            });

            options.AddOperationTransformer((operation, _, _) =>
            {
                foreach (IOpenApiParameter parameter in operation.Parameters ?? [])
                {
                    if (parameter is OpenApiParameter header && header.Name == HttpHeaderNames.IdempotencyKey)
                    {
                        header.Description = OpenApiDocumentation.IdempotencyKeyDescription;
                    }
                }

                return Task.CompletedTask;
            });
        }
    }
}
