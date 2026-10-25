using Quotes.Domain.Common;

namespace Quotes.Application.Abstractions
{
    /// <summary>
    /// Handles a request that changes state (create a quote, change its status).
    /// </summary>
    public interface ICommandHandler<in TCommand, TResult>
    {
        Task<Result<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken);
    }
}
