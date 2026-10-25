using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Quotes.Infrastructure.Messaging;
using Testcontainers.PostgreSql;

namespace Quotes.IntegrationTests.Infrastructure
{
    /// <summary>
    /// Runs the real API in memory against a real PostgreSQL in a container (Docker must be running).
    /// The background jobs are switched off so tests drive the outbox dispatcher and maintenance themselves,
    /// and the broker is replaced by a recording publisher.
    /// </summary>
    public sealed class QuotesApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private const string PostgresImage = "postgres:17.11-alpine";

        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder(PostgresImage)
            .WithDatabase("advisor_quotes")
            .WithUsername("advisor_quotes")
            .WithPassword("integration_tests")
            .Build();

        public string ConnectionString => _postgres.GetConnectionString();

        public RecordingIntegrationEventPublisher Publisher { get; } = new RecordingIntegrationEventPublisher();

        public async Task InitializeAsync()
        {
            await _postgres.StartAsync();
            await DatabaseScripts.ApplyAsync(ConnectionString);
        }

        async Task IAsyncLifetime.DisposeAsync()
        {
            await DisposeAsync();
            await _postgres.DisposeAsync();
        }

        public async Task<long> CountRowsAsync(string table, string adviserId)
        {
            await using NpgsqlConnection connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            await using NpgsqlCommand command = new NpgsqlCommand($"SELECT count(*) FROM {table} WHERE adviser_id = @adviserId", connection);
            command.Parameters.AddWithValue("adviserId", adviserId);

            return (long)(await command.ExecuteScalarAsync() ?? 0L);
        }

        public async Task<List<OutboxRow>> GetOutboxRowsAsync(Guid quoteId)
        {
            await using NpgsqlConnection connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            await using NpgsqlCommand command = new NpgsqlCommand(
                """
                SELECT id, event_type, payload::text, trace_parent, attempts, processed_at, last_error, locked_until, lock_token
                FROM outbox_messages
                WHERE payload->>'quoteId' = @quoteId
                ORDER BY occurred_at, id
                """,
                connection);
            command.Parameters.AddWithValue("quoteId", quoteId.ToString());

            List<OutboxRow> rows = [];
            await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                rows.Add(new OutboxRow
                {
                    Id = reader.GetGuid(0),
                    EventType = reader.GetString(1),
                    Payload = reader.GetString(2),
                    TraceParent = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Attempts = reader.GetInt32(4),
                    ProcessedAt = reader.IsDBNull(5) ? null : reader.GetFieldValue<DateTimeOffset>(5),
                    LastError = reader.IsDBNull(6) ? null : reader.GetString(6),
                    LockedUntil = reader.IsDBNull(7) ? null : reader.GetFieldValue<DateTimeOffset>(7),
                    LockToken = reader.IsDBNull(8) ? null : reader.GetGuid(8)
                });
            }

            return rows;
        }

        public async Task<int> ExecuteSqlAsync(string sql, params (string Name, object Value)[] parameters)
        {
            await using NpgsqlConnection connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            await using NpgsqlCommand command = new NpgsqlCommand(sql, connection);

            foreach ((string name, object value) in parameters)
            {
                command.Parameters.AddWithValue(name, value);
            }

            return await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Tries to lock an outbox row from a separate connection without waiting (FOR UPDATE NOWAIT).
        /// False means another transaction is holding a lock on it.
        /// </summary>
        public async Task<bool> TryLockOutboxRowAsync(Guid messageId)
        {
            await using NpgsqlConnection connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync();

            await using NpgsqlCommand command = new NpgsqlCommand(
                "SELECT id FROM outbox_messages WHERE id = @id FOR UPDATE NOWAIT", connection, transaction);
            command.Parameters.AddWithValue("id", messageId);

            try
            {
                await command.ExecuteScalarAsync();

                return true;
            }
            catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.LockNotAvailable)
            {
                return false;
            }
            finally
            {
                await transaction.RollbackAsync();
            }
        }

        public async Task<T> InScopeAsync<T>(Func<IServiceProvider, Task<T>> action)
        {
            using IServiceScope scope = Services.CreateScope();

            return await action(scope.ServiceProvider);
        }

        /// <summary>
        /// Publishes every pending message that can be published now (failing ones wait for their retry).
        /// </summary>
        public async Task DrainOutboxAsync()
        {
            OutboxDispatchResult result;

            do
            {
                result = await InScopeAsync(services => services.GetRequiredService<OutboxDispatcher>().DispatchPendingAsync(CancellationToken.None));
            }
            while (result.Published > 0);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:QuotesDb", ConnectionString);
            builder.UseSetting("Telemetry:ConsoleExporter", "false");
            builder.UseSetting("OpenApi:Enabled", "true");

            // The tests share one client address, so the normal limit would throttle the suite itself.
            // ApiPlatformTests lowers it on purpose.
            builder.UseSetting("RateLimiting:PermitLimit", "100000");

            // The tests run the dispatcher and maintenance themselves, and retry failed messages straight away.
            builder.UseSetting("Outbox:Enabled", "false");
            builder.UseSetting("Outbox:InitialRetryDelay", "00:00:00");
            builder.UseSetting("Maintenance:Enabled", "false");

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IIntegrationEventPublisher>();
                services.AddSingleton<IIntegrationEventPublisher>(Publisher);
            });
        }
    }

    public sealed record OutboxRow
    {
        public required Guid Id { get; init; }

        public required string EventType { get; init; }

        public required string Payload { get; init; }

        public string? TraceParent { get; init; }

        public required int Attempts { get; init; }

        public DateTimeOffset? ProcessedAt { get; init; }

        public string? LastError { get; init; }

        public DateTimeOffset? LockedUntil { get; init; }

        public Guid? LockToken { get; init; }
    }

    [CollectionDefinition(Name)]
    public sealed class ApiCollection : ICollectionFixture<QuotesApiFactory>
    {
        public const string Name = "api";
    }
}
