using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Quotes.Application.Abstractions;
using Quotes.Application.Idempotency;
using Quotes.Application.Models.Commands;
using Quotes.Application.Models.Response.V1;
using Quotes.Application.Models.Results;
using Quotes.Domain.Common;

namespace Quotes.Application.Observability
{
    /// <summary>
    /// Decorator that adds the quote.create span, business metrics and logs around the create use case, so the
    /// handler itself only contains business logic (single responsibility; open for extension without modification).
    /// </summary>
    public sealed class InstrumentedCreateQuoteHandler : ICommandHandler<CreateQuoteCommand, CreateQuoteResult>
    {
        private readonly ICommandHandler<CreateQuoteCommand, CreateQuoteResult> _inner;

        private readonly QuotesMetrics _metrics;

        private readonly ILogger<InstrumentedCreateQuoteHandler> _logger;

        public InstrumentedCreateQuoteHandler(
            ICommandHandler<CreateQuoteCommand, CreateQuoteResult> inner,
            QuotesMetrics metrics,
            ILogger<InstrumentedCreateQuoteHandler> logger)
        {
            _inner = inner;
            _metrics = metrics;
            _logger = logger;
        }

        public async Task<Result<CreateQuoteResult>> HandleAsync(CreateQuoteCommand command, CancellationToken cancellationToken)
        {
            using Activity? activity = QuotesTelemetry.ActivitySource.StartActivity(QuotesTelemetry.SpanNames.CreateQuote);
            activity?.SetTag(QuotesTelemetry.TagNames.AdviserId, command.Request.AdviserId);

            Result<CreateQuoteResult> result = await _inner.HandleAsync(command, cancellationToken);

            if (result.IsFailure)
            {
                Error error = result.Error!;
                QuotesTelemetry.RecordFailure(activity, error);
                _metrics.RequestRejected(QuotesMetrics.Operations.CreateQuote, error);
                _logger.RequestRejected(QuotesMetrics.Operations.CreateQuote, error.Code, error.Message);

                return result;
            }

            QuoteResponse quote = result.Value.Quote;
            string keySource = result.Value.KeySource == IdempotencyKeySource.Client
                ? QuotesMetrics.KeySources.Client
                : QuotesMetrics.KeySources.Fingerprint;

            activity?.SetTag(QuotesTelemetry.TagNames.IdempotencyKeySource, keySource);
            activity?.SetTag(QuotesTelemetry.TagNames.QuoteId, quote.Id);
            activity?.SetTag(QuotesTelemetry.TagNames.QuoteReference, quote.ExternalReference);
            activity?.SetTag(QuotesTelemetry.TagNames.IdempotencyReplayed, result.Value.IsReplay);

            if (result.Value.IsReplay)
            {
                _metrics.DuplicateReplayed(keySource);
                _logger.QuoteReplayed(quote.AdviserId, quote.Id, keySource);
            }
            else
            {
                _metrics.QuoteCreated(quote.Product.ProductCode, quote.Premium.MonthlyAmount, quote.Premium.Currency);
                _logger.QuoteCreated(
                    quote.Id,
                    quote.ExternalReference,
                    quote.AdviserId,
                    quote.Premium.MonthlyAmount,
                    quote.Premium.Currency,
                    quote.Premium.PricingVersion);
            }

            return result;
        }
    }
}
