namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration.LocalIntegration.Tests.Infrastructure;

using Npgsql;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Databases;
using Xunit;

/// <summary>Owns one PostgreSQL database whose lifetime and name belong to the current test.</summary>
internal sealed class PostgreSqlTestDatabase : IAsyncDisposable
{
    private readonly NpgsqlConnectionStringBuilder _admin;

    private PostgreSqlTestDatabase(
        NpgsqlConnectionStringBuilder admin,
        string databaseName,
        string connectionString,
        TimeSpan operationTimeout)
    {
        _admin = admin;
        DatabaseName = databaseName;
        ConnectionString = connectionString;
        OperationTimeout = operationTimeout;
    }

    public string ConnectionString { get; }

    public string DatabaseName { get; }

    public TimeSpan OperationTimeout { get; }

    public static async Task<PostgreSqlTestDatabase> CreateAsync(
        string purpose,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        ViciOneTestOptions options = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedLocalOptions(LocalTestResource.PostgreSql);
        PostgreSqlLocalOptions postgreSql = options.LocalInfrastructure!.PostgreSql!;
        TimeSpan operationTimeout = options.OperationTimeout!.Value;
        int timeoutSeconds = Math.Max(1, (int)Math.Ceiling(operationTimeout.TotalSeconds));
        var admin = new NpgsqlConnectionStringBuilder
        {
            Host = postgreSql.Host,
            Port = postgreSql.Port!.Value,
            Database = postgreSql.Database,
            Username = postgreSql.UserName,
            Password = postgreSql.Password,
            Pooling = false,
            Timeout = timeoutSeconds,
            CommandTimeout = timeoutSeconds,
        };
        string runIdentity = TestContext.Current.Test?.UniqueID
            ?? throw new InvalidOperationException("A PostgreSQL test database requires an active xUnit test identity.");
        string databaseName = TestDatabaseName.CreateForCurrentRun("vsbpg", runIdentity, purpose);

        await using (var connection = new NpgsqlConnection(admin.ConnectionString))
        {
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand(
                $"CREATE DATABASE {QuoteIdentifier(databaseName)}",
                connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        string connectionString = new NpgsqlConnectionStringBuilder(admin.ConnectionString)
        {
            Database = databaseName,
        }.ConnectionString;

        return new PostgreSqlTestDatabase(admin, databaseName, connectionString, operationTimeout);
    }

    public async ValueTask DisposeAsync()
    {
        await using var connection = new NpgsqlConnection(_admin.ConnectionString);
        await connection.OpenAsync(CancellationToken.None);
        await using var command = new NpgsqlCommand(
            $"DROP DATABASE IF EXISTS {QuoteIdentifier(DatabaseName)} WITH (FORCE)",
            connection);
        await command.ExecuteNonQueryAsync(CancellationToken.None);
    }

    private static string QuoteIdentifier(string identifier) =>
        $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
}
