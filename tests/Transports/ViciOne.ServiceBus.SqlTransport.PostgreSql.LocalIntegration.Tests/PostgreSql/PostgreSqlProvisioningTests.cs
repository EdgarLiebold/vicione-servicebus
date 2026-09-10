using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;
using ViciOne.ServiceBus.SqlTransport;
using ViciOne.ServiceBus.SqlTransport.PostgreSql;
using ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.PostgreSql;

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
    public async Task Provisioning_CreatesEveryRequiredTableAndFetchIndexAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync(
            "provision-schema",
            cancellationToken);
        await using NpgsqlConnection connection = fixture.CreateConnection();
        await connection.OpenWithinAsync(fixture.OperationTimeout, cancellationToken);

        IReadOnlyList<string> tables = await connection.SchemaTablesAsync(fixture.Schema, cancellationToken);
        IReadOnlyList<string> indices = await connection.SchemaIndicesAsync(fixture.Schema, cancellationToken);

        Assert.Equal(RequiredTables, tables);
        Assert.Contains("message_delivery_fetch_ndx", indices);
        Assert.Contains("unique_queue", indices);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0038", "postgresql-native-owner")]
    public async Task Disposal_DropsTheRunScopedDatabaseEvenWithAnAttachedSessionAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync("drop-database", cancellationToken);
        string database = fixture.Database;
        string serverConnectionString = fixture.ServerConnectionString;
        await using NpgsqlConnection attached = fixture.CreateConnection();
        await attached.OpenWithinAsync(fixture.OperationTimeout, cancellationToken);

        await fixture.DisposeAsync();
        await using var server = new NpgsqlConnection(serverConnectionString);
        await server.OpenAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);

        Assert.False(await server.DatabaseExistsAsync(database, cancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-POSTGRES-ROUTINE-NAMES", "provisioning-removes-versioned-and-overloaded-routines")]
    public async Task Provisioning_ExposesOneUnversionedRoutineForEachClientEntryPointAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync(
            "unversioned-routines",
            cancellationToken);
        await using NpgsqlConnection connection = fixture.CreateConnection();
        await connection.OpenWithinAsync(fixture.OperationTimeout, cancellationToken);

        IReadOnlyList<string> routineNames = await RoutineNamesAsync(connection, fixture.Schema, cancellationToken);

        Assert.Equal(1, routineNames.Count(name => name == "create_queue"));
        Assert.Equal(1, routineNames.Count(name => name == "send_message"));
        Assert.Equal(1, routineNames.Count(name => name == "publish_message"));
        Assert.DoesNotContain(routineNames, name => name.EndsWith("_v2", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0131", "postgresql-native-owner")]
    public async Task InfrastructureProvisioning_ASecondRunPreservesTheCompleteObjectSetAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync(
            "idempotent-infrastructure",
            cancellationToken);
        await using NpgsqlConnection before = fixture.CreateConnection();
        await before.OpenWithinAsync(fixture.OperationTimeout, cancellationToken);
        IReadOnlyList<string> tables = await before.SchemaTablesAsync(fixture.Schema, cancellationToken);
        IReadOnlyList<string> indices = await before.SchemaIndicesAsync(fixture.Schema, cancellationToken);
        long routines = await RoutineCountAsync(before, fixture.Schema, cancellationToken);
        var migrator = new PostgreSqlDatabaseMigrator(NullLogger<PostgreSqlDatabaseMigrator>.Instance);

        await migrator.CreateInfrastructureAsync(fixture.Options, cancellationToken)
            .WaitAsync(fixture.OperationTimeout, cancellationToken);

        await using NpgsqlConnection after = fixture.CreateConnection();
        await after.OpenWithinAsync(fixture.OperationTimeout, cancellationToken);
        Assert.Equal(tables, await after.SchemaTablesAsync(fixture.Schema, cancellationToken));
        Assert.Equal(indices, await after.SchemaIndicesAsync(fixture.Schema, cancellationToken));
        Assert.Equal(routines, await RoutineCountAsync(after, fixture.Schema, cancellationToken));
        Assert.True(routines > 0);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-POSTGRES-NOTIFICATION-TRIGGER", "every-transport-schema-owns-its-trigger")]
    public async Task InfrastructureProvisioning_CreatesANotificationTriggerInEveryTransportSchemaAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync(
            "multi-schema-trigger",
            cancellationToken);
        const string secondSchema = "transport_secondary";
        var secondOptions = new SqlTransportOptions
        {
            Host = fixture.Options.Host,
            Port = fixture.Options.Port,
            Database = fixture.Options.Database,
            Schema = secondSchema,
            Role = fixture.Options.Role,
            Username = fixture.Options.Username,
            Password = fixture.Options.Password,
            AdminUsername = fixture.Options.AdminUsername,
            AdminPassword = fixture.Options.AdminPassword,
        };
        var migrator = new PostgreSqlDatabaseMigrator(NullLogger<PostgreSqlDatabaseMigrator>.Instance);

        await migrator.CreateSchemaIfNotExistAsync(secondOptions, cancellationToken)
            .WaitAsync(fixture.OperationTimeout, cancellationToken);
        await migrator.CreateInfrastructureAsync(secondOptions, cancellationToken)
            .WaitAsync(fixture.OperationTimeout, cancellationToken);

        await using NpgsqlConnection connection = fixture.CreateConnection();
        await connection.OpenWithinAsync(fixture.OperationTimeout, cancellationToken);
        IReadOnlyList<string> triggerSchemas = await NotificationTriggerSchemasAsync(
            connection,
            [fixture.Schema, secondSchema],
            cancellationToken);

        Assert.Equal([fixture.Schema, secondSchema], triggerSchemas);
    }

    [Theory]
    [InlineData(true, true, true, "database,schema,infrastructure")]
    [InlineData(true, false, false, "database")]
    [InlineData(false, true, false, "schema")]
    [InlineData(false, false, true, "infrastructure")]
    [RequirementCoverage("OBL-R0-SQL-0130", "postgresql-native-owner")]
    public async Task MigrationStartup_InvokesOnlyTheIndependentlyEnabledStagesInStableOrderAsync(
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

    private static async Task<long> RoutineCountAsync(
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

    private static async Task<IReadOnlyList<string>> RoutineNamesAsync(
        NpgsqlConnection connection,
        string schema,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            "SELECT routine_name FROM information_schema.routines WHERE routine_schema = @schema ORDER BY routine_name",
            connection);
        command.Parameters.AddWithValue("schema", schema);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        var names = new List<string>();

        while (await reader.ReadAsync(cancellationToken))
            names.Add(reader.GetString(0));

        return names;
    }

    private static async Task<IReadOnlyList<string>> NotificationTriggerSchemasAsync(
        NpgsqlConnection connection,
        string[] schemas,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            "SELECT n.nspname FROM pg_trigger t "
            + "INNER JOIN pg_class c ON c.oid = t.tgrelid "
            + "INNER JOIN pg_namespace n ON n.oid = c.relnamespace "
            + "WHERE NOT t.tgisinternal AND t.tgname = 'message_delivery_notify_trigger' "
            + "AND n.nspname = ANY(@schemas) ORDER BY n.nspname",
            connection);
        command.Parameters.AddWithValue("schemas", schemas);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        var names = new List<string>();

        while (await reader.ReadAsync(cancellationToken))
            names.Add(reader.GetString(0));

        return names;
    }

    private sealed class RecordingMigrator : ISqlTransportDatabaseMigrator
    {
        public List<MigrationCall> Calls { get; } = [];

        public Task CreateDatabaseAsync(SqlTransportOptions options, CancellationToken cancellationToken = default) =>
            RecordAsync("database", options, cancellationToken);

        public Task CreateSchemaIfNotExistAsync(SqlTransportOptions options, CancellationToken cancellationToken = default) =>
            RecordAsync("schema", options, cancellationToken);

        public Task CreateInfrastructureAsync(SqlTransportOptions options, CancellationToken cancellationToken = default) =>
            RecordAsync("infrastructure", options, cancellationToken);

        public Task DeleteDatabaseAsync(SqlTransportOptions options, CancellationToken cancellationToken = default) =>
            RecordAsync("delete", options, cancellationToken);

        private Task RecordAsync(string stage, SqlTransportOptions options, CancellationToken cancellationToken)
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
