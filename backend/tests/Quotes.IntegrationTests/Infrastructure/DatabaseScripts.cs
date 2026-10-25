using Npgsql;

namespace Quotes.IntegrationTests.Infrastructure
{
    /// <summary>
    /// Applies src/dbQuotes/up/*.sql in name order, the same scripts grate runs, so the tests prove the EF mapping matches the schema.
    /// </summary>
    internal static class DatabaseScripts
    {
        private const string SolutionFileName = "AdvisorQuotes.slnx";

        public static async Task ApplyAsync(string connectionString)
        {
            string scriptsDirectory = Path.Combine(FindSolutionDirectory(), "src", "dbQuotes", "up");
            IEnumerable<string> scripts = Directory.GetFiles(scriptsDirectory, "*.sql").OrderBy(path => path, StringComparer.Ordinal);

            await using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            foreach (string script in scripts)
            {
                await using NpgsqlCommand command = new NpgsqlCommand(await File.ReadAllTextAsync(script), connection);
                await command.ExecuteNonQueryAsync();
            }
        }

        private static string FindSolutionDirectory()
        {
            DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, SolutionFileName)))
            {
                directory = directory.Parent;
            }

            return directory?.FullName ?? throw new InvalidOperationException($"Could not find {SolutionFileName} above {AppContext.BaseDirectory}.");
        }
    }
}
