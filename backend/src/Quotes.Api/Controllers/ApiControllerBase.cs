using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Quotes.Api.Constants;
using Quotes.Domain.Common;

namespace Quotes.Api.Controllers
{
    [ApiController]
    public abstract class ApiControllerBase : ControllerBase
    {
        /// <summary>
        /// Turns an Error into an RFC 9457 problem response. The error type decides the status code, and the stable
        /// error code is added as a "code" extension so clients can act on it.
        /// </summary>
        protected ObjectResult Problem(Error error)
        {
            int statusCode = error.Type switch
            {
                ErrorType.Validation => StatusCodes.Status400BadRequest,
                ErrorType.NotFound => StatusCodes.Status404NotFound,
                ErrorType.Conflict => StatusCodes.Status409Conflict,
                ErrorType.Unprocessable => StatusCodes.Status422UnprocessableEntity,
                _ => StatusCodes.Status500InternalServerError
            };

            ProblemDetails problem = error.ValidationErrors is null
                ? ProblemDetailsFactory.CreateProblemDetails(
                    HttpContext,
                    statusCode: statusCode,
                    title: ReasonPhrases.GetReasonPhrase(statusCode),
                    detail: error.Message)
                : ProblemDetailsFactory.CreateValidationProblemDetails(
                    HttpContext,
                    ToModelState(error.ValidationErrors),
                    statusCode: statusCode,
                    detail: error.Message);

            problem.Extensions[ProblemDetailsKeys.ErrorCode] = error.Code;

            ObjectResult result = new ObjectResult(problem) { StatusCode = statusCode };
            result.ContentTypes.Add(MediaTypes.ProblemJson);

            return result;
        }

        private static ModelStateDictionary ToModelState(IReadOnlyDictionary<string, string[]> validationErrors)
        {
            ModelStateDictionary modelState = new ModelStateDictionary();

            foreach (KeyValuePair<string, string[]> field in validationErrors)
            {
                foreach (string message in field.Value)
                {
                    modelState.AddModelError(field.Key, message);
                }
            }

            return modelState;
        }
    }
}
