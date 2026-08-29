namespace ViciOne.ServiceBus.SqlTransport.SqlServer.LocalIntegration.Tests.Infrastructure;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.ServiceBus.SqlTransport.SqlServer;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Databases;

internal sealed class SqlServerTestDatabase : IAsyncDisposable
{
    private readonly SqlServerDatabaseMigrator _migrator;
    private bool _databaseCreated;

    private SqlServerTestDatabase(
        SqlTransportOptions options,
        string serverConnectionString,
        TimeSpan operationTimeout,
        string prefix)
    {
        Options = options;
        ServerConnectionString = serverConnectionString;
        OperationTimeout = operationTimeout;
        Prefix = prefix;
        _migrator = new SqlServerDatabaseMigrator(NullLogger<SqlServerDatabaseMigrator>.Instance);
    }

    public SqlTransportOptions Options { get; }

    public string ConnectionString => SqlServerSqlTransportConnection.CreateBuilder(Options).ConnectionString;

    public string ServerConnectionString { get; }

    public string Database => Options.Database!;

    public string Schema => Options.Schema!;

    public TimeSpan OperationTimeout { get; }

    public string Prefix { get; }

    public static async Task<SqlServerTestDatabase> CreateAsync(
        string purpose,
        CancellationToken cancellationToken,
        string schema = "transport")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        ArgumentException.ThrowIfNullOrWhiteSpace(schema);

        ViciOneTestOptions testOptions = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedLocalOptions(LocalTestResource.SqlServer);
        SqlServerLocalOptions provider = testOptions.LocalInfrastructure!.SqlServer!;
        string database = TestDatabaseName.CreateForCurrentRun(
            "vsbms",
            typeof(SqlServerTestDatabase).Assembly.GetName().Name!,
            $"{purpose}-{Guid.NewGuid():N}");
        var transportOptions = new SqlTransportOptions
        {
            Host = provider.Host,
            Port = provider.Port,
            Database = database,
            Schema = schema,
            Role = "transport",
            Username = provider.UserName,
            Password = provider.Password,
            AdminUsername = provider.UserName,
            AdminPassword = provider.Password,
        };
        var serverBuilder = new SqlConnectionStringBuilder
        {
            DataSource = $"{provider.Host},{provider.Port}",
            InitialCatalog = provider.Database,
            UserID = provider.UserName,
            Password = provider.Password,
            TrustServerCertificate = true,
        };
        var fixture = new SqlServerTestDatabase(
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

    public SqlConnection CreateConnection() => new(ConnectionString);

    public void ConfigureHost(ISqlBusFactoryConfigurator configurator) =>
        configurator.UseSqlServer(ConnectionString, host => host.Schema = Schema);

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
