namespace ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.Infrastructure;

using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using ViciOne.ServiceBus.SqlTransport.PostgreSql;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Databases;

internal sealed class PostgreSqlTestDatabase : IAsyncDisposable
{
    private readonly PostgresDatabaseMigrator _migrator;
    private bool _databaseCreated;

    private PostgreSqlTestDatabase(
        SqlTransportOptions options,
        string serverConnectionString,
        TimeSpan operationTimeout,
        string prefix)
    {
        Options = options;
        ServerConnectionString = serverConnectionString;
        OperationTimeout = operationTimeout;
        Prefix = prefix;
        _migrator = new PostgresDatabaseMigrator(NullLogger<PostgresDatabaseMigrator>.Instance);
    }

    public SqlTransportOptions Options { get; }

    public string ConnectionString => PostgresSqlTransportConnection.CreateBuilder(Options).ConnectionString;

    public string ServerConnectionString { get; }

    public string Database => Options.Database!;

    public string Schema => Options.Schema!;

    public TimeSpan OperationTimeout { get; }

    public string Prefix { get; }

    public static async Task<PostgreSqlTestDatabase> CreateAsync(
        string purpose,
        CancellationToken cancellationToken,
        string schema = "transport")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        ArgumentException.ThrowIfNullOrWhiteSpace(schema);

        ViciOneTestOptions testOptions = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedLocalOptions(LocalTestResource.PostgreSql);
        PostgreSqlLocalOptions provider = testOptions.LocalInfrastructure!.PostgreSql!;
        string database = TestDatabaseName.CreateForCurrentRun(
            "vsbpg",
            typeof(PostgreSqlTestDatabase).Assembly.GetName().Name!,
            $"{purpose}-{Guid.NewGuid():N}");
        var transportOptions = new SqlTransportOptions
        {
            Host = provider.Host,
            Port = provider.Port,
            Database = database,
            Schema = schema,
            Role = provider.UserName,
            Username = provider.UserName,
            Password = provider.Password,
            AdminUsername = provider.UserName,
            AdminPassword = provider.Password,
        };
        var serverBuilder = new NpgsqlConnectionStringBuilder
        {
            Host = provider.Host,
            Port = provider.Port!.Value,
            Database = provider.Database,
            Username = provider.UserName,
            Password = provider.Password,
        };
        var fixture = new PostgreSqlTestDatabase(
            transportOptions,
            serverBuilder.ConnectionString,
            testOptions.OperationTimeout!.Value,
            NamePart(purpose));

        try
        {
            await fixture.ProvisionAsync(cancellationToken);
            return fixture;
        }
        catch
        {
            await fixture.DisposeAsync();
            throw;
        }
    }

    public string Name(string purpose)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        return $"{Prefix}-{NamePart(purpose)}-{Guid.NewGuid():N}";
    }

    public NpgsqlConnection CreateConnection() => new(ConnectionString);

    public NpgsqlDataSource CreateDataSource() => NpgsqlDataSource.Create(ConnectionString);

    public void ConfigureHost(ISqlBusFactoryConfigurator configurator) => configurator.UsePostgres(ConnectionString);

    public async ValueTask DisposeAsync()
    {
        if (!_databaseCreated)
            return;

        _databaseCreated = false;
        using var timeout = new CancellationTokenSource(OperationTimeout);
        await _migrator.DeleteDatabase(Options, timeout.Token);
    }

    private async Task ProvisionAsync(CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(OperationTimeout);
        await _migrator.CreateDatabase(Options, timeout.Token);
        _databaseCreated = true;
        await _migrator.CreateSchemaIfNotExist(Options, timeout.Token);
        await _migrator.CreateInfrastructure(Options, timeout.Token);
    }

    private static string NamePart(string value)
    {
        string result = new(value.ToLowerInvariant()
            .Where(character => char.IsAsciiLetterOrDigit(character) || character == '-')
            .ToArray());
        if (result.Length == 0)
            throw new ArgumentException("The value must contain a database-name character.", nameof(value));
        return result.Length <= 20 ? result : result[..20];
    }
}
