using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quotes.Application.Constants;
using Quotes.Domain.Entities;

namespace Quotes.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Maps the Quote aggregate onto src/dbQuotes/up/0001_create_quotes.sql (plus adviser_reference from 0005). Column names are explicit so the mapping is easy
    /// to check against the SQL; the integration tests run against those same scripts to catch any drift.
    /// </summary>
    internal sealed class QuoteConfiguration : IEntityTypeConfiguration<Quote>
    {
        /// <summary>
        /// Shadow property mapped to PostgreSQL's xmin system column: optimistic concurrency without a column on the entity.
        /// </summary>
        public const string RowVersionProperty = "RowVersion";

        private const int StatusLength = 20;

        private const int ExternalReferenceLength = 30;

        private const int PricingVersionLength = 20;

        private const int MoneyPrecision = 18;

        private const int MoneyScale = 2;

        public void Configure(EntityTypeBuilder<Quote> builder)
        {
            builder.ToTable("quotes");

            builder.HasKey(quote => quote.Id).HasName("pk_quotes");

            builder.Property(quote => quote.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            builder.Property(quote => quote.ExternalReference)
                .HasColumnName("external_reference")
                .HasMaxLength(ExternalReferenceLength);

            builder.Property(quote => quote.AdviserId)
                .HasColumnName("adviser_id")
                .HasMaxLength(FieldLengths.AdviserId);

            builder.Property(quote => quote.AdviserReference)
                .HasColumnName("adviser_reference")
                .HasMaxLength(FieldLengths.AdviserReference);

            builder.ComplexProperty(quote => quote.Client, client =>
            {
                client.Property(details => details.FullName)
                    .HasColumnName("client_name")
                    .HasMaxLength(FieldLengths.ClientName);

                client.Property(details => details.DateOfBirth)
                    .HasColumnName("client_date_of_birth");

                client.Property(details => details.IsSmoker)
                    .HasColumnName("client_is_smoker");
            });

            builder.ComplexProperty(quote => quote.Product, product =>
            {
                product.Property(details => details.ProductCode)
                    .HasColumnName("product_code")
                    .HasMaxLength(FieldLengths.ProductCode);

                product.Property(details => details.CoverAmount)
                    .HasColumnName("cover_amount")
                    .HasPrecision(MoneyPrecision, MoneyScale);

                product.Property(details => details.TermYears)
                    .HasColumnName("term_years")
                    .HasConversion<short>();
            });

            builder.ComplexProperty(quote => quote.MonthlyPremium, premium =>
            {
                premium.Property(money => money.Amount)
                    .HasColumnName("monthly_premium")
                    .HasPrecision(MoneyPrecision, MoneyScale);

                premium.Property(money => money.Currency)
                    .HasColumnName("currency")
                    .HasColumnType("char(3)");
            });

            builder.Property(quote => quote.PricingVersion)
                .HasColumnName("pricing_version")
                .HasMaxLength(PricingVersionLength);

            builder.Property(quote => quote.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(StatusLength);

            builder.Property(quote => quote.Notes)
                .HasColumnName("notes")
                .HasMaxLength(FieldLengths.Notes);

            builder.Property(quote => quote.CreatedAt)
                .HasColumnName("created_at");

            builder.Property(quote => quote.UpdatedAt)
                .HasColumnName("updated_at");

            builder.Property<uint>(RowVersionProperty)
                .HasColumnName("xmin")
                .IsRowVersion();

            builder.HasMany(quote => quote.StatusHistory)
                .WithOne()
                .HasForeignKey(change => change.QuoteId)
                .HasConstraintName("fk_quote_status_changes_quote_id");

            builder.Navigation(quote => quote.StatusHistory)
                .HasField("_statusHistory")
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            // Domain events are not columns: the unit of work moves them to the outbox when the quote is saved.
            builder.Ignore(quote => quote.DomainEvents);
        }
    }
}
