using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Quotes.Api.Logging;

namespace Quotes.Api.ErrorHandling
{
    /// <summary>
    /// Last line of defence: logs the exception and returns a generic 500 problem response without internal details.
    /// Expected failures never get here; handlers return them as Results.
    /// </summary>
    internal sealed class GlobalExceptionHandler : IExceptionHandler
    {
        private const string UnexpectedErrorTitle = "An unexpected error occurred.";

        private readonly IProblemDetailsService _problemDetailsService;

        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
        {
            _problemDetailsService = problemDetailsService;
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            _logger.UnhandledException(exception, httpContext.Request.Method, httpContext.Request.Path.ToString());

            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

            return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = UnexpectedErrorTitle
                }
            });
        }
    }
}
