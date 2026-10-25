namespace Quotes.Application.Constants
{
    /// <summary>
    /// Maximum lengths shared by request validation and the database column definitions.
    /// </summary>
    public static class FieldLengths
    {
        public const int AdviserId = 50;

        public const int AdviserReference = 50;

        public const int ClientName = 200;

        public const int ProductCode = 20;

        public const int Notes = 2000;

        public const int StatusReason = 500;

        public const int ChangedBy = 100;

        public const int IdempotencyKey = 100;
    }
}
