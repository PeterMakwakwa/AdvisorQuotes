using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quotes.Application.Constants;
using Quotes.Domain.Entities;

namespace Quotes.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Maps src/dbQuotes/up/0002_create_quote_status_changes.sql.
    /// </summary>
    internal sealed class StatusChangeConfiguration : IEntityTypeConfiguration<StatusChange>
    {
        private const int StatusLength = 20;

        public void Configure(EntityTypeBuilder<StatusChange> builder)
        {
            builder.ToTable("quote_status_changes");

            builder.HasKey(change => change.Id).HasName("pk_quote_status_changes");

            // The id is a version 7 GUID created by the domain. ValueGeneratedNever tells EF that a new entry with an id
            // already set is an insert, not an update of an existing row.
            builder.Property(change => change.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            builder.Property(change => change.QuoteId)
                .HasColumnName("quote_id");

            builder.Property(change => change.FromStatus)
                .HasColumnName("from_status")
                .HasConversion<string>()
                .HasMaxLength(StatusLength);

            builder.Property(change => change.ToStatus)
                .HasColumnName("to_status")
                .HasConversion<string>()
                .HasMaxLength(StatusLength);

            builder.Property(change => change.Reason)
                .HasColumnName("reason")
                .HasMaxLength(FieldLengths.StatusReason);

            builder.Property(change => change.ChangedBy)
                .HasColumnName("changed_by")
                .HasMaxLength(FieldLengths.ChangedBy);

            builder.Property(change => change.ChangedAt)
                .HasColumnName("changed_at");
        }
    }
}
