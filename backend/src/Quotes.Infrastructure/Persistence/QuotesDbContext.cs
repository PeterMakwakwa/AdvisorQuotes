using Microsoft.EntityFrameworkCore;
using Quotes.Application.Idempotency;
using Quotes.Domain.Entities;
using Quotes.Infrastructure.Messaging;

namespace Quotes.Infrastructure.Persistence
{
    /// <summary>
    /// EF Core is used for data access only. The schema is owned by the grate scripts in src/dbQuotes/up, not by EF migrations.
    /// </summary>
    public sealed class QuotesDbContext : DbContext
    {
        public QuotesDbContext(DbContextOptions<QuotesDbContext> options)
            : base(options)
        {
        }

        public DbSet<Quote> Quotes => Set<Quote>();

        public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(QuotesDbContext).Assembly);

            modelBuilder.HasDbFunction(typeof(PostgresFunctions).GetMethod(nameof(PostgresFunctions.Upper))!)
                .HasName("upper")
                .IsBuiltIn()
                .IsNullable(false);
        }
    }
}
