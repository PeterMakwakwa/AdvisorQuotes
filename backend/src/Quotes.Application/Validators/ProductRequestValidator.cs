using FluentValidation;
using Quotes.Application.Constants;
using Quotes.Application.Models.Request.V1;

namespace Quotes.Application.Validators
{
    /// <summary>
    /// Checks the request shape only. Product-specific limits (cover range, term range) are pricing rules in the domain.
    /// </summary>
    public sealed class ProductRequestValidator : AbstractValidator<ProductRequest>
    {
        private const int MoneyPrecision = 18;

        private const int MoneyScale = 2;

        public ProductRequestValidator()
        {
            RuleFor(product => product.ProductCode)
                .NotEmpty()
                .MaximumLength(FieldLengths.ProductCode);

            RuleFor(product => product.CoverAmount)
                .GreaterThan(0)
                .PrecisionScale(MoneyPrecision, MoneyScale, ignoreTrailingZeros: true);

            RuleFor(product => product.TermYears)
                .GreaterThan(0);
        }
    }
}
