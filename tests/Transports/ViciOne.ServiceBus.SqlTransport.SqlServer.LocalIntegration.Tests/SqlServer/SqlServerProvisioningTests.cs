using Microsoft.Data.SqlClient;
using ViciOne.ServiceBus.SqlTransport.SqlServer;
using ViciOne.ServiceBus.SqlTransport.SqlServer.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.SqlServer.LocalIntegration.Tests.SqlServer;

public sealed class SqlServerProvisioningTests
{
    private static readonly string[] RequiredTables =
    [
        "message",
        "messagedelivery",
        "queue",
        "queuemetric",
        "queuemetriccapture",
        "queuesubscription",
        "topic",
        "topicsubscription",
    ];

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0040", "sqlserver-native-owner")]
    public async Task ExplicitInstanceAndPort_AreProjectedIntoConnectionAndBusAddressesAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using SqlServerTestDatabase fixture = await SqlServerTestDatabase.CreateAsync(
            "instance-port",
            cancellationToken);
        var projectedOptions = new SqlTransportOptions
        {
            Host = "localhost\\instance",
            Port = 3381,
            Database = fixture.Database,
            Schema = fixture.Schema,
            Role = fixture.Options.Role,
            Username = fixture.Options.Username,
            Password = fixture.Options.Password,
        };
        var settings = new SqlServerSqlHostSettings(projectedOptions);
        IBusControl bus = SqlBusFactory.Create(fixture.ConfigureHost);
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;

            Assert.Equal("localhost\\instance,3381", SqlServerSqlTransportConnection.CreateBuilder(projectedOptions).DataSource);
            Assert.Equal("localhost", settings.HostAddress.Host);
            Assert.Equal(3381, settings.HostAddress.Port);
            Assert.Equal("instance", QueryValue(settings.HostAddress, "instance"));
            Assert.Equal(fixture.Options.Host, bus.Address.Host);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0041", "sqlserver-native-owner")]
    public async Task InstanceWithoutPort_IsProjectedOnlyAsTheBusQueryOptionAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using SqlServerTestDatabase fixture = await SqlServerTestDatabase.CreateAsync(
            "instance-only",
            cancellationToken);
        var projectedOptions = new SqlTransportOptions
        {
            Host = "localhost\\instance",
            Database = fixture.Database,
            Schema = fixture.Schema,
            Role = fixture.Options.Role,
            Username = fixture.Options.Username,
            Password = fixture.Options.Password,
        };
        var settings = new SqlServerSqlHostSettings(projectedOptions);
        IBusControl bus = SqlBusFactory.Create(fixture.ConfigureHost);
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;

            Assert.Equal("localhost\\instance", SqlServerSqlTransportConnection.CreateBuilder(projectedOptions).DataSource);
            Assert.Equal("db://localhost", settings.HostAddress.GetLeftPart(UriPartial.Authority));
            Assert.Equal("instance", QueryValue(settings.HostAddress, "instance"));
            Assert.Equal(fixture.Options.Host, bus.Address.Host);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0045", "sqlserver-native-owner")]
    public async Task Provisioning_CreatesEveryRequiredTableAndIndexAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using SqlServerTestDatabase fixture = await SqlServerTestDatabase.CreateAsync(
            "provision-schema",
            cancellationToken);
        await using SqlConnection connection = fixture.CreateConnection();
        await connection.OpenWithinAsync(fixture.OperationTimeout, cancellationToken);

        IReadOnlyList<string> tables = await connection.SchemaTablesAsync(fixture.Schema, cancellationToken);
        IReadOnlyList<string> indices = await connection.SchemaIndicesAsync(fixture.Schema, cancellationToken);

        Assert.Equal(RequiredTables, tables);
        Assert.Contains("ix_messagedelivery_fetch", indices);
        Assert.Contains("ix_queue_name_type", indices);
        Assert.Contains("ix_messagedelivery_transportmessageid", indices);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0046", "sqlserver-native-owner")]
    public async Task Disposal_DropsTheRunScopedDatabaseEvenWithAnAttachedSessionAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        SqlServerTestDatabase fixture = await SqlServerTestDatabase.CreateAsync("drop-database", cancellationToken);
        string database = fixture.Database;
        string serverConnectionString = fixture.ServerConnectionString;
        await using SqlConnection attached = fixture.CreateConnection();
        await attached.OpenWithinAsync(fixture.OperationTimeout, cancellationToken);

        await fixture.DisposeAsync();
        await using var server = new SqlConnection(serverConnectionString);
        await server.OpenWithinAsync(fixture.OperationTimeout, cancellationToken);

        Assert.False(await server.DatabaseExistsAsync(database, cancellationToken));
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0047", "sqlserver-native-owner")]
    public async Task ReceiveEndpointWithoutTopology_CreatesItsThreeQueueRowsAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using SqlServerTestDatabase fixture = await SqlServerTestDatabase.CreateAsync(
            "receive-endpoint",
            cancellationToken);
        string queueName = fixture.Name("input");
        IBusControl bus = SqlBusFactory.Create(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.ConfigureConsumeTopology = false;
                endpoint.Handler<ProvisioningMessage>(_ => Task.CompletedTask);
            });
        });
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            await using SqlConnection connection = fixture.CreateConnection();
            await connection.OpenWithinAsync(fixture.OperationTimeout, cancellationToken);

            Assert.True(await connection.QueueExistsAsync(fixture.Schema, queueName, 1, cancellationToken));
            Assert.True(await connection.QueueExistsAsync(fixture.Schema, queueName, 2, cancellationToken));
            Assert.True(await connection.QueueExistsAsync(fixture.Schema, queueName, 3, cancellationToken));
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0127", "sqlserver-native-owner")]
    public async Task RunScopedHost_IsPreservedInTheProviderDataSourceAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using SqlServerTestDatabase fixture = await SqlServerTestDatabase.CreateAsync(
            "host-projection",
            cancellationToken);
        await using SqlConnection connection = fixture.CreateConnection();
        var builder = new SqlConnectionStringBuilder(connection.ConnectionString);

        await connection.OpenWithinAsync(fixture.OperationTimeout, cancellationToken);

        Assert.Equal($"{fixture.Options.Host},{fixture.Options.Port}", builder.DataSource);
        Assert.Equal(fixture.Database, builder.InitialCatalog);
        Assert.Equal(fixture.Options.Username, builder.UserID);
        Assert.Equal(System.Data.ConnectionState.Open, connection.State);
    }

    private static string? QueryValue(Uri address, string name) =>
        address.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(value => value.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .FirstOrDefault(parts => parts[0].Equals(name, StringComparison.OrdinalIgnoreCase))?[1];

    public sealed record ProvisioningMessage(Guid Id);
}
