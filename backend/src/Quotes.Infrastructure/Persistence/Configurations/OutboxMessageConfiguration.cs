using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quotes.Infrastructure.Messaging;

namespace Quotes.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Maps src/dbQuotes/up/0007_create_outbox_messages.sql and 0008_add_lease_to_outbox_messages.sql.
    /// </summary>
    internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
    {
        private const int EventTypeLength = 100;

        private const int TraceParentLength = 55;

        private const int LastErrorLength = 2000;

        public void Configure(EntityTypeBuilder<OutboxMessage> builder)
        {
            builder.ToTable("outbox_messages");

            builder.HasKey(message => message.Id).HasName("pk_outbox_messages");

            builder.Property(message => message.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            builder.Property(message => message.EventType)
                .HasColumnName("event_type")
                .HasMaxLength(EventTypeLength);

            builder.Property(message => message.Payload)
                .HasColumnName("payload")
                .HasColumnType("jsonb");

            builder.Property(message => message.OccurredAt)
                .HasColumnName("occurred_at");

            builder.Property(message => message.TraceParent)
                .HasColumnName("trace_parent")
                .HasMaxLength(TraceParentLength);

            builder.Property(message => message.Attempts)
                .HasColumnName("attempts");

            builder.Property(message => message.NextAttemptAt)
                .HasColumnName("next_attempt_at");

            builder.Property(message => message.ProcessedAt)
                .HasColumnName("processed_at");

            builder.Property(message => message.LastError)
                .HasColumnName("last_error")
                .HasMaxLength(LastErrorLength);

            builder.Property(message => message.LockedUntil)
                .HasColumnName("locked_until");

            // Saving an outcome adds "AND lock_token = <the token we claimed with>", so a dispatcher whose lease expired
            // (and was taken over) gets a concurrency conflict instead of overwriting the new holder's result.
            builder.Property(message => message.LockToken)
                .HasColumnName("lock_token")
                .IsConcurrencyToken();
        }
    }
}
