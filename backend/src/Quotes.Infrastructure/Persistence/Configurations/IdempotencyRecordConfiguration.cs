using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quotes.Application.Constants;
using Quotes.Application.Idempotency;
using Quotes.Domain.Entities;

namespace Quotes.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Maps src/dbQuotes/up/0003_create_idempotency_keys.sql.
    /// </summary>
    internal sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
    {
        /// <summary>
        /// The unit of work looks for a unique violation on this constraint to detect a concurrent duplicate submission.
        /// </summary>
        public const string PrimaryKeyName = "pk_idempotency_keys";

        private const int RequestHashLength = 64;

        public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
        {
            builder.ToTable("idempotency_keys");

            builder.HasKey(record => new { record.AdviserId, record.Key }).HasName(PrimaryKeyName);

            builder.Property(record => record.AdviserId)
                .HasColumnName("adviser_id")
                .HasMaxLength(FieldLengths.AdviserId);

            builder.Property(record => record.Key)
                .HasColumnName("idempotency_key")
                .HasMaxLength(FieldLengths.IdempotencyKey);

            builder.Property(record => record.RequestHash)
                .HasColumnName("request_hash")
                .HasColumnType(ColumnTypes.Sha256Hex)
                .HasMaxLength(RequestHashLength);

            builder.Property(record => record.QuoteId)
                .HasColumnName("quote_id");

            builder.Property(record => record.CreatedAt)
                .HasColumnName("created_at");

            builder.Property(record => record.ExpiresAt)
                .HasColumnName("expires_at");

            builder.Property<uint>(ColumnTypes.RowVersionProperty)
                .HasColumnName(ColumnTypes.RowVersionColumn)
                .IsRowVersion();

            // The relationship makes EF insert the quote before the record that points at it.
            builder.HasOne<Quote>()
                .WithMany()
                .HasForeignKey(record => record.QuoteId)
                .HasConstraintName("fk_idempotency_keys_quote_id");
        }
    }
}
