namespace ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.PostgreSql;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;
using ViciOne.ServiceBus.SqlTransport;
using ViciOne.ServiceBus.SqlTransport.PostgreSql;
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

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0131", "postgresql-native-owner")]
    public async Task InfrastructureProvisioning_ASecondRunPreservesTheCompleteObjectSet()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync(
            "idempotent-infrastructure",
            cancellationToken);
        await using NpgsqlConnection before = fixture.CreateConnection();
        await before.OpenWithin(fixture.OperationTimeout, cancellationToken);
        IReadOnlyList<string> tables = await before.SchemaTables(fixture.Schema, cancellationToken);
        IReadOnlyList<string> indices = await before.SchemaIndices(fixture.Schema, cancellationToken);
        long routines = await RoutineCount(before, fixture.Schema, cancellationToken);
        var migrator = new PostgresDatabaseMigrator(NullLogger<PostgresDatabaseMigrator>.Instance);

        await migrator.CreateInfrastructure(fixture.Options, cancellationToken)
            .WaitAsync(fixture.OperationTimeout, cancellationToken);

        await using NpgsqlConnection after = fixture.CreateConnection();
        await after.OpenWithin(fixture.OperationTimeout, cancellationToken);
        Assert.Equal(tables, await after.SchemaTables(fixture.Schema, cancellationToken));
        Assert.Equal(indices, await after.SchemaIndices(fixture.Schema, cancellationToken));
        Assert.Equal(routines, await RoutineCount(after, fixture.Schema, cancellationToken));
        Assert.True(routines > 0);
    }

    [Theory]
    [InlineData(true, true, true, "database,schema,infrastructure")]
    [InlineData(true, false, false, "database")]
    [InlineData(false, true, false, "schema")]
    [InlineData(false, false, true, "infrastructure")]
    [RequirementCoverage("OBL-R0-SQL-0130", "postgresql-native-owner")]
    public async Task MigrationStartup_InvokesOnlyTheIndependentlyEnabledStagesInStableOrder(
        bool createDatabase,
        bool createSchema,
        bool createInfrastructure,
        string expectedCalls)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var migrator = new RecordingMigrator();
        var transportOptions = new SqlTransportOptions { Database = "owner-database" };
        var migrationOptions = new SqlTransportMigrationOptions
        {
            CreateDatabase = createDatabase,
            CreateSchema = createSchema,
            CreateInfrastructure = createInfrastructure,
        };
        var service = new SqlTransportMigrationHostedService(
            migrator,
            NullLogger<SqlTransportMigrationHostedService>.Instance,
            Options.Create(migrationOptions),
            Options.Create(transportOptions));

        await service.StartAsync(cancellationToken);

        Assert.Equal(expectedCalls.Split(','), migrator.Calls.Select(call => call.Stage));
        Assert.All(migrator.Calls, call =>
        {
            Assert.Same(transportOptions, call.Options);
            Assert.Equal(cancellationToken, call.CancellationToken);
        });
    }

    private static async Task<long> RoutineCount(
        NpgsqlConnection connection,
        string schema,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            "SELECT COUNT(*) FROM information_schema.routines WHERE routine_schema = @schema",
            connection);
        command.Parameters.AddWithValue("schema", schema);
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
    }

    private sealed class RecordingMigrator : ISqlTransportDatabaseMigrator
    {
        public List<MigrationCall> Calls { get; } = [];

        public Task CreateDatabase(SqlTransportOptions options, CancellationToken cancellationToken = default) =>
            Record("database", options, cancellationToken);

        public Task CreateSchemaIfNotExist(SqlTransportOptions options, CancellationToken cancellationToken = default) =>
            Record("schema", options, cancellationToken);

        public Task CreateInfrastructure(SqlTransportOptions options, CancellationToken cancellationToken = default) =>
            Record("infrastructure", options, cancellationToken);

        public Task DeleteDatabase(SqlTransportOptions options, CancellationToken cancellationToken = default) =>
            Record("delete", options, cancellationToken);

        private Task Record(string stage, SqlTransportOptions options, CancellationToken cancellationToken)
        {
            Calls.Add(new MigrationCall(stage, options, cancellationToken));
            return Task.CompletedTask;
        }
    }

    private sealed record MigrationCall(
        string Stage,
        SqlTransportOptions Options,
        CancellationToken CancellationToken);
}
