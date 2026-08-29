namespace ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.PostgreSql;

using Npgsql;
using ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class PostgreSqlProvisioningTests
{
    private static readonly string[] RequiredTables =
    [
        "message",
        "message_delivery",
        "queue",
        "queue_metric",
        "queue_metric_capture",
        "queue_subscription",
        "topic",
        "topic_subscription",
    ];

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0037", "postgresql-native-owner")]
    public async Task Provisioning_CreatesEveryRequiredTableAndFetchIndex()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync(
            "provision-schema",
            cancellationToken);
        await using NpgsqlConnection connection = fixture.CreateConnection();
        await connection.OpenWithin(fixture.OperationTimeout, cancellationToken);

        IReadOnlyList<string> tables = await connection.SchemaTables(fixture.Schema, cancellationToken);
        IReadOnlyList<string> indices = await connection.SchemaIndices(fixture.Schema, cancellationToken);

        Assert.Equal(RequiredTables, tables);
        Assert.Contains("message_delivery_fetch_ndx", indices);
        Assert.Contains("unique_queue", indices);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0038", "postgresql-native-owner")]
    public async Task Disposal_DropsTheRunScopedDatabaseEvenWithAnAttachedSession()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync("drop-database", cancellationToken);
        string database = fixture.Database;
        string serverConnectionString = fixture.ServerConnectionString;
        await using NpgsqlConnection attached = fixture.CreateConnection();
        await attached.OpenWithin(fixture.OperationTimeout, cancellationToken);

        await fixture.DisposeAsync();
        await using var server = new NpgsqlConnection(serverConnectionString);
        await server.OpenAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);

        Assert.False(await server.DatabaseExists(database, cancellationToken));
    }
}
