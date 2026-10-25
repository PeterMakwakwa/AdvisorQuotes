using System.Text.Json;
using FluentValidation.Results;
using Quotes.Domain.Common;

namespace Quotes.Application.Common
{
    internal static class ValidationExtensions
    {
        /// <summary>
        /// Converts FluentValidation failures into a validation Error, with field paths in camelCase to match the JSON contract.
        /// </summary>
        public static Error ToError(this ValidationResult validationResult)
        {
            Dictionary<string, string[]> errors = validationResult.Errors
                .GroupBy(failure => ToCamelCasePath(failure.PropertyName))
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(failure => failure.ErrorMessage).Distinct().ToArray());

            return Error.Validation(errors);
        }

        private static string ToCamelCasePath(string propertyName)
        {
            return string.Join('.', propertyName.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));
        }
    }
}
