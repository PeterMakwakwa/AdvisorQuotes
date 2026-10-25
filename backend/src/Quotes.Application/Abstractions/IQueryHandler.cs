using Quotes.Domain.Common;

namespace Quotes.Application.Abstractions
{
    /// <summary>
    /// Handles a read-only request (view a quote, list quotes). Never changes state.
    /// </summary>
    public interface IQueryHandler<in TQuery, TResult>
    {
        Task<Result<TResult>> HandleAsync(TQuery query, CancellationToken cancellationToken);
    }
}
