using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Quotes.Application.Exceptions;
using Quotes.Domain.Common;
using Quotes.Domain.Observability.Messages;
using Quotes.Domain.Repositories;
using Quotes.Infrastructure.Messaging;
using Quotes.Infrastructure.Persistence.Configurations;

namespace Quotes.Infrastructure.Persistence
{
    /// <summary>
    /// SaveChanges runs every pending insert and update in one transaction, including the outbox messages for the domain
    /// events the aggregates raised, so a change and its events are stored together or not at all. Database-specific
    /// failures are turned into Application exceptions, so the use cases never depend on EF Core or Npgsql types.
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
            List<AggregateRoot> aggregates = MoveDomainEventsToOutbox();

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
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

            foreach (AggregateRoot aggregate in aggregates)
            {
                aggregate.ClearDomainEvents();
            }
        }

        private List<AggregateRoot> MoveDomainEventsToOutbox()
        {
            string? traceParent = Activity.Current?.Id;

            List<AggregateRoot> aggregates = _dbContext.ChangeTracker.Entries<AggregateRoot>()
                .Select(entry => entry.Entity)
                .Where(aggregate => aggregate.DomainEvents.Count > 0)
                .ToList();

            foreach (IDomainEvent domainEvent in aggregates.SelectMany(aggregate => aggregate.DomainEvents))
            {
                _dbContext.OutboxMessages.Add(IntegrationEvents.ToOutboxMessage(domainEvent, traceParent));
            }

            return aggregates;
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
