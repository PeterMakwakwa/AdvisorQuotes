using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace Quotes.IntegrationTests.Infrastructure
{
    /// <summary>
    /// Makes the next commit fail with a transient connection error after the database has in fact committed: the case
    /// where the application cannot tell whether its transaction was saved.
    /// </summary>
    public sealed class CommitThenDropConnectionInterceptor : DbTransactionInterceptor
    {
        private int _armed = 1;

        public bool Triggered => Volatile.Read(ref _armed) == 0;

        public override Task TransactionCommittedAsync(
            DbTransaction transaction,
            TransactionEndEventData eventData,
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref _armed, 0) == 1)
            {
                throw new NpgsqlException("Connection dropped after commit (simulated by the test).", new IOException("simulated"));
            }

            return Task.CompletedTask;
        }
    }
}
