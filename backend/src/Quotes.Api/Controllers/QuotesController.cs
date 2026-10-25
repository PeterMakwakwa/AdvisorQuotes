using System.Globalization;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Quotes.Api.Constants;
using Quotes.Api.RateLimiting;
using Quotes.Application.Abstractions;
using Quotes.Application.Models.Commands;
using Quotes.Application.Models.Queries;
using Quotes.Application.Models.Request.V1;
using Quotes.Application.Models.Response.V1;
using Quotes.Application.Models.Results;
using Quotes.Domain.Common;

namespace Quotes.Api.Controllers
{
    /// <summary>
    /// Thin HTTP layer: map the request to a command or query, call its handler, and map the Result to HTTP.
    /// </summary>
    [ApiVersion(1.0)]
    [Route("api/v{version:apiVersion}/quotes")]
    [Produces("application/json")]
    [EnableRateLimiting(RateLimitingExtensions.QuotesPolicy)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, "application/problem+json")]
    public sealed class QuotesController : ApiControllerBase
    {
        private const string TrueHeaderValue = "true";

        private const string DefaultVersion = "1";

        private readonly ICommandHandler<CreateQuoteCommand, CreateQuoteResult> _createQuoteHandler;

        private readonly ICommandHandler<ChangeQuoteStatusCommand, QuoteResponse> _changeQuoteStatusHandler;

        private readonly IQueryHandler<GetQuoteQuery, QuoteResponse> _getQuoteHandler;

        private readonly IQueryHandler<ListQuotesQuery, PagedResponse<QuoteSummaryResponse>> _listQuotesHandler;

        public QuotesController(
            ICommandHandler<CreateQuoteCommand, CreateQuoteResult> createQuoteHandler,
            ICommandHandler<ChangeQuoteStatusCommand, QuoteResponse> changeQuoteStatusHandler,
            IQueryHandler<GetQuoteQuery, QuoteResponse> getQuoteHandler,
            IQueryHandler<ListQuotesQuery, PagedResponse<QuoteSummaryResponse>> listQuotesHandler)
        {
            _createQuoteHandler = createQuoteHandler;
            _changeQuoteStatusHandler = changeQuoteStatusHandler;
            _getQuoteHandler = getQuoteHandler;
            _listQuotesHandler = listQuotesHandler;
        }

        /// <summary>
        /// Captures a quote and calculates its premium. Send an Idempotency-Key header to make retries safe; a repeat
        /// returns the original quote with the header Idempotent-Replayed: true.
        /// </summary>
        [HttpPost]
        [ProducesResponseType<QuoteResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
        public async Task<IActionResult> Create(
            [FromHeader(Name = HttpHeaderNames.IdempotencyKey)] string? idempotencyKey,
            [FromBody] CreateQuoteRequest request,
            CancellationToken cancellationToken)
        {
            Result<CreateQuoteResult> result = await _createQuoteHandler.HandleAsync(
                new CreateQuoteCommand { IdempotencyKey = idempotencyKey, Request = request },
                cancellationToken);

            if (result.IsFailure)
            {
                return Problem(result.Error!);
            }

            if (result.Value.IsReplay)
            {
                Response.Headers[HttpHeaderNames.IdempotentReplayed] = TrueHeaderValue;
            }

            string version = HttpContext.Features.Get<IApiVersioningFeature>()?.RequestedApiVersion?.MajorVersion?.ToString(CultureInfo.InvariantCulture)
                ?? DefaultVersion;

            return CreatedAtAction(nameof(GetById), new { version, quoteId = result.Value.Quote.Id }, result.Value.Quote);
        }

        /// <summary>
        /// Lists quotes, newest first, optionally filtered by adviser and status.
        /// </summary>
        [HttpGet]
        [ProducesResponseType<PagedResponse<QuoteSummaryResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
        public async Task<IActionResult> List([FromQuery] ListQuotesRequest request, CancellationToken cancellationToken)
        {
            Result<PagedResponse<QuoteSummaryResponse>> result = await _listQuotesHandler.HandleAsync(request.BuildQuery(), cancellationToken);

            return result.IsSuccess ? Ok(result.Value) : Problem(result.Error!);
        }

        /// <summary>
        /// Returns one quote with its status history and the statuses it can move to next.
        /// </summary>
        [HttpGet("{quoteId:guid}")]
        [ProducesResponseType<QuoteResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
        public async Task<IActionResult> GetById(Guid quoteId, CancellationToken cancellationToken)
        {
            Result<QuoteResponse> result = await _getQuoteHandler.HandleAsync(new GetQuoteQuery { QuoteId = quoteId }, cancellationToken);

            return result.IsSuccess ? Ok(result.Value) : Problem(result.Error!);
        }

        /// <summary>
        /// Moves a quote to a new status and records the change in its history.
        /// </summary>
        [HttpPost("{quoteId:guid}/status-changes")]
        [ProducesResponseType<QuoteResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
        public async Task<IActionResult> ChangeStatus(
            Guid quoteId,
            [FromBody] ChangeQuoteStatusRequest request,
            CancellationToken cancellationToken)
        {
            Result<QuoteResponse> result = await _changeQuoteStatusHandler.HandleAsync(
                new ChangeQuoteStatusCommand { QuoteId = quoteId, Request = request },
                cancellationToken);

            return result.IsSuccess ? Ok(result.Value) : Problem(result.Error!);
        }
    }
}
