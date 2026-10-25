namespace Quotes.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// PostgreSQL column types and system columns used by the entity configurations.
    /// </summary>
    internal static class ColumnTypes
    {
        public const string Sha256Hex = "char(64)";

        public const string CurrencyCode = "char(3)";

        public const string Json = "jsonb";

        /// <summary>
        /// PostgreSQL's xmin system column, mapped to the <see cref="RowVersionProperty"/> shadow property for optimistic
        /// concurrency without a column on the entity.
        /// </summary>
        public const string RowVersionColumn = "xmin";

        public const string RowVersionProperty = "RowVersion";
    }
}
