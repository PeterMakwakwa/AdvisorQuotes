using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using Quotes.Application.Exceptions;
using Quotes.Application.Interfaces;
using Quotes.Domain.Common;
using Quotes.Domain.Observability.Messages;
using Quotes.Infrastructure.Messaging;
using Quotes.Infrastructure.Persistence.Configurations;

namespace Quotes.Infrastructure.Persistence
{
    /// <summary>
    /// SaveChanges runs every pending insert and update in one transaction, including the outbox messages for the domain
    /// events the aggregates raised, so a change and its events are stored together or not at all. Database-specific
    /// failures are turned into Application exceptions, so the use cases never depend on EF Core or Npgsql types.
    /// <para>
    /// The save runs through the execution strategy with a commit check. If the connection drops while committing, the
    /// strategy cannot tell whether the commit happened; it asks <see cref="WasCommittedAsync"/>, which looks for this save's
    /// first outbox message (every change raises an event, so that row only exists if the transaction committed). Without
    /// the check the retry would insert the same rows again and fail on their primary keys, after the change was saved.
    /// </para>
    /// </summary>
    internal sealed class UnitOfWork : IUnitOfWork
    {
        private readonly QuotesDbContext _dbContext;

        public UnitOfWork(QuotesDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            (List<AggregateRoot> aggregates, Guid? commitMarker) = MoveDomainEventsToOutbox();
            IExecutionStrategy strategy = _dbContext.Database.CreateExecutionStrategy();

            try
            {
                // acceptAllChangesOnSuccess: false keeps every entry Added/Modified until the commit is known to have
                // happened, so a retry after a rolled-back attempt writes the same changes again.
                await strategy.ExecuteInTransactionAsync(
                    commitMarker,
                    (_, token) => _dbContext.SaveChangesAsync(acceptAllChangesOnSuccess: false, token),
                    WasCommittedAsync,
                    cancellationToken);
            }
            catch (DbUpdateConcurrencyException exception)
            {
                // Forget the failed changes so the caller can re-read current data on this same context.
                _dbContext.ChangeTracker.Clear();

                throw new ConcurrencyConflictException(ErrorMessages.ConcurrencyConflict, exception);
            }
            catch (DbUpdateException exception) when (IsDuplicateIdempotencyKey(exception))
            {
                _dbContext.ChangeTracker.Clear();

                throw new DuplicateIdempotencyKeyException(ErrorMessages.DuplicateIdempotencyKey, exception);
            }

            _dbContext.ChangeTracker.AcceptAllChanges();

            foreach (AggregateRoot aggregate in aggregates)
            {
                aggregate.ClearDomainEvents();
            }
        }

        private (List<AggregateRoot> Aggregates, Guid? CommitMarker) MoveDomainEventsToOutbox()
        {
            string? traceParent = Activity.Current?.Id;
            Guid? commitMarker = null;

            List<AggregateRoot> aggregates = _dbContext.ChangeTracker.Entries<AggregateRoot>()
                .Select(entry => entry.Entity)
                .Where(aggregate => aggregate.DomainEvents.Count > 0)
                .ToList();

            foreach (IDomainEvent domainEvent in aggregates.SelectMany(aggregate => aggregate.DomainEvents))
            {
                OutboxMessage message = IntegrationEvents.ToOutboxMessage(domainEvent, traceParent);
                _dbContext.OutboxMessages.Add(message);
                commitMarker ??= message.Id;
            }

            return (aggregates, commitMarker);
        }

        /// <summary>
        /// Asked only when the commit itself failed: true if the transaction committed anyway. A save without events has
        /// no marker and is treated as not committed, so it is retried (and fails loudly if it had in fact committed).
        /// </summary>
        private async Task<bool> WasCommittedAsync(Guid? commitMarker, CancellationToken cancellationToken)
        {
            return commitMarker is not null
                && await _dbContext.OutboxMessages.AsNoTracking().AnyAsync(message => message.Id == commitMarker.Value, cancellationToken);
        }

        private static bool IsDuplicateIdempotencyKey(DbUpdateException exception)
        {
            return exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: IdempotencyRecordConfiguration.PrimaryKeyName
            };
        }
    }
}
