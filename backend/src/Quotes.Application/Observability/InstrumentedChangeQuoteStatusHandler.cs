using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Quotes.Application.Abstractions;
using Quotes.Application.Models.Commands;
using Quotes.Application.Models.Response.V1;
using Quotes.Domain.Common;

namespace Quotes.Application.Observability
{
    /// <summary>
    /// Decorator that adds the quote.status.change span, business metrics and logs around the status-change use case.
    /// </summary>
    public sealed class InstrumentedChangeQuoteStatusHandler : ICommandHandler<ChangeQuoteStatusCommand, QuoteResponse>
    {
        private readonly ICommandHandler<ChangeQuoteStatusCommand, QuoteResponse> _inner;

        private readonly QuotesMetrics _metrics;

        private readonly ILogger<InstrumentedChangeQuoteStatusHandler> _logger;

        public InstrumentedChangeQuoteStatusHandler(
            ICommandHandler<ChangeQuoteStatusCommand, QuoteResponse> inner,
            QuotesMetrics metrics,
            ILogger<InstrumentedChangeQuoteStatusHandler> logger)
        {
            _inner = inner;
            _metrics = metrics;
            _logger = logger;
        }

        public async Task<Result<QuoteResponse>> HandleAsync(ChangeQuoteStatusCommand command, CancellationToken cancellationToken)
        {
            using Activity? activity = QuotesTelemetry.ActivitySource.StartActivity(QuotesTelemetry.SpanNames.ChangeQuoteStatus);
            activity?.SetTag(QuotesTelemetry.TagNames.QuoteId, command.QuoteId);
            activity?.SetTag(QuotesTelemetry.TagNames.StatusTo, command.Request.NewStatus.ToString());
            activity?.SetTag(QuotesTelemetry.TagNames.ChangedBy, command.Request.ChangedBy);

            Result<QuoteResponse> result = await _inner.HandleAsync(command, cancellationToken);

            if (result.IsFailure)
            {
                Error error = result.Error!;
                QuotesTelemetry.RecordFailure(activity, error);
                _metrics.RequestRejected(QuotesMetrics.Operations.ChangeStatus, error);
                _logger.RequestRejected(QuotesMetrics.Operations.ChangeStatus, error.Code, error.Message);

                return result;
            }

            QuoteResponse quote = result.Value;

            // The change that produced the current status. The lifecycle never returns to an earlier status, so this is
            // unique and doesn't depend on how history entries with equal timestamps are ordered.
            StatusChangeResponse change = quote.StatusHistory.Last(entry => entry.ToStatus == quote.Status);

            activity?.SetTag(QuotesTelemetry.TagNames.StatusFrom, change.FromStatus?.ToString());
            activity?.SetTag(QuotesTelemetry.TagNames.QuoteReference, quote.ExternalReference);

            if (change.FromStatus is not null)
            {
                _metrics.StatusChanged(change.FromStatus.Value, change.ToStatus);
            }

            _logger.QuoteStatusChanged(quote.Id, change.FromStatus, change.ToStatus, change.ChangedBy);

            return result;
        }
    }
}
