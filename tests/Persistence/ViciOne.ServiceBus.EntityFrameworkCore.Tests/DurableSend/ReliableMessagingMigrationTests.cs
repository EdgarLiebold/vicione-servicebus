using System.Reflection;
using Microsoft.Data.Sqlite;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Tests.DurableSend;

public sealed class ReliableMessagingMigrationTests
{
    private const string LegacyMessageType = "urn:message:ViciOne.Tests:OrderAccepted";
    private const string ContractIdentity = "vicione.tests.order-accepted@1";
    private const string StoreKey = "orders-primary";

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-RELIABLE-MIGRATION", "sqlite-preserves-retry-quarantine-inbox-and-capacity-idempotently")]
    public async Task SqliteMigration_PreservesRetainedIntentAndCanResumeIdempotentlyAsync()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await CreateLegacySchemaAsync(connection);

        Guid retryId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid quarantineId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        Guid deliveredId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        Guid retryOutboxId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        Guid quarantineOutboxId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        Guid deliveredOutboxId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        Guid consumerId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

        await ExecuteAsync(connection, $"""
            INSERT INTO OutboxState
                (OutboxId, BusKey, Status, NextDeliveryTime, DeliveryAttempts, LastFailureKind, LastFailureTime, LastFailure)
            VALUES
                ('{retryOutboxId}', '{StoreKey}', 1, '2026-09-03T12:05:00Z', 2, 1, '2026-09-03T12:04:00Z', 'network'),
                ('{quarantineOutboxId}', '{StoreKey}', 3, NULL, 4, 3, '2026-09-03T12:06:00Z', 'invariant'),
                ('{deliveredOutboxId}', '{StoreKey}', 2, NULL, 1, 0, NULL, NULL);

            INSERT INTO OutboxMessage
                (MessageId, OutboxId, MessageType, DestinationAddress, ContentType, Body, CorrelationId, SentTime, EnqueueTime)
            VALUES
                ('{retryId}', '{retryOutboxId}', '{LegacyMessageType}', 'rabbitmq://orders/input', 'application/json', 'payload1', NULL, '2026-09-03T12:00:00Z', NULL),
                ('{quarantineId}', '{quarantineOutboxId}', '{LegacyMessageType}', 'rabbitmq://orders/input', 'application/json', 'payload2', '{retryId}', '2026-09-03T12:01:00Z', '2026-09-03T12:02:00Z'),
                ('{deliveredId}', '{deliveredOutboxId}', '{LegacyMessageType}', 'rabbitmq://orders/input', 'application/json', 'payload3', NULL, '2026-09-03T12:02:00Z', NULL);

            INSERT INTO InboxState (MessageId, ConsumerId, Received, ReceiveCount, Consumed)
            VALUES
                ('{retryId}', '{consumerId}', '2026-09-03T11:59:00Z', 3, NULL),
                ('{quarantineId}', '{consumerId}', '2026-09-03T11:58:00Z', 1, '2026-09-03T12:03:00Z');
            """);

        string script = ReadScript("reliable-messaging-sqlite.sql");
        script = Configure(script, StoreKey, LegacyMessageType, ContractIdentity);
        await ExecuteAsync(connection, script);
        await ExecuteAsync(connection, script);

        await using (SqliteCommand command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT Id, ContractIdentity, DestinationAddress, hex(Body), MessageId, CorrelationId,
                       StorageSize, Status, DueAt, DeliveryAttempts, NextAttemptAt,
                       LastFailureKind, LastFailureType
                FROM vicione_outbox ORDER BY Id;
                """;
            await using SqliteDataReader reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);

            Assert.True(await reader.ReadAsync(TestContext.Current.CancellationToken));
            Assert.Equal(retryId.ToString(), reader.GetString(0));
            Assert.Equal(ContractIdentity, reader.GetString(1));
            Assert.Equal("rabbitmq://orders/input", reader.GetString(2));
            Assert.Equal("7061796C6F616431", reader.GetString(3));
            Assert.Equal(retryId.ToString(), reader.GetString(4));
            Assert.True(reader.IsDBNull(5));
            Assert.Equal(8, reader.GetInt64(6));
            Assert.Equal(1, reader.GetInt32(7));
            Assert.True(reader.IsDBNull(8));
            Assert.Equal(2, reader.GetInt32(9));
            Assert.Equal("2026-09-03T12:05:00Z", reader.GetString(10));
            Assert.Equal(1, reader.GetInt32(11));
            Assert.True(reader.IsDBNull(12));

            Assert.True(await reader.ReadAsync(TestContext.Current.CancellationToken));
            Assert.Equal(quarantineId.ToString(), reader.GetString(0));
            Assert.Equal(retryId.ToString(), reader.GetString(5));
            Assert.Equal(2, reader.GetInt32(7));
            Assert.Equal("2026-09-03T12:02:00Z", reader.GetString(8));
            Assert.Equal(4, reader.GetInt32(9));
            Assert.Equal(5, reader.GetInt32(11));
            Assert.Equal("invariant", reader.GetString(12));
            Assert.False(await reader.ReadAsync(TestContext.Current.CancellationToken));
        }

        Assert.Equal((2L, 16L), await ReadPairAsync(
            connection,
            $"SELECT StoredCount, StoredBytes FROM vicione_reliable_capacity WHERE StoreKey = '{StoreKey}'"));
        Assert.Equal((2L, 4L), await ReadPairAsync(
            connection,
            $"SELECT COUNT(*), SUM(Attempts) FROM vicione_inbox WHERE StoreKey = '{StoreKey}'"));
        Assert.Equal(1L, await ScalarInt64Async(
            connection,
            $"SELECT COUNT(*) FROM vicione_inbox WHERE StoreKey = '{StoreKey}' AND Status = 1"));
        Assert.Equal(3L, await ScalarInt64Async(
            connection,
            "SELECT COUNT(*) FROM OutboxMessage"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-RELIABLE-MIGRATION", "sqlite-fails-closed-on-unmapped-contract-or-wrong-bus")]
    public async Task SqliteMigration_FailsBeforeCreatingCanonicalTablesWhenIntentCannotBeProvenAsync()
    {
        await AssertRejectedAsync(messageType: "unmapped", busKey: StoreKey);
        await AssertRejectedAsync(messageType: LegacyMessageType, busKey: "different-bus");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-RELIABLE-MIGRATION", "all-provider-scripts-are-explicit-and-retain-legacy-tables")]
    public void ProviderScripts_RequireExplicitIdentityMappingAndDoNotDropLegacyTables()
    {
        foreach (string provider in new[] { "sqlite", "postgresql", "sqlserver" })
        {
            string script = ReadScript($"reliable-messaging-{provider}.sql");
            Assert.Contains("__VICIONE_STORE_KEY__", script, StringComparison.Ordinal);
            Assert.Contains("__LEGACY_MESSAGE_TYPE__", script, StringComparison.Ordinal);
            Assert.Contains("__CONTRACT_IDENTITY__", script, StringComparison.Ordinal);
            Assert.Contains("vicione_outbox", script, StringComparison.Ordinal);
            Assert.Contains("vicione_inbox", script, StringComparison.Ordinal);
            Assert.Contains("vicione_reliable_capacity", script, StringComparison.Ordinal);
            Assert.DoesNotContain("DROP TABLE OutboxMessage", script, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("DROP TABLE OutboxState", script, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("DROP TABLE InboxState", script, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static async Task AssertRejectedAsync(string messageType, string busKey)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await CreateLegacySchemaAsync(connection);
        await ExecuteAsync(connection, $"""
            INSERT INTO OutboxState (OutboxId, BusKey, Status, DeliveryAttempts, LastFailureKind)
            VALUES ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '{busKey}', 0, 0, 0);
            INSERT INTO OutboxMessage
                (MessageId, OutboxId, MessageType, DestinationAddress, ContentType, Body, SentTime)
            VALUES
                ('11111111-1111-1111-1111-111111111111', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
                 '{messageType}', 'rabbitmq://orders/input', 'application/json', 'payload', '2026-09-03T12:00:00Z');
            """);

        string script = Configure(ReadScript("reliable-messaging-sqlite.sql"), StoreKey, LegacyMessageType, ContractIdentity);
        await Assert.ThrowsAsync<SqliteException>(() => ExecuteAsync(connection, script));
        Assert.Equal(0L, await ScalarInt64Async(
            connection,
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'vicione_outbox'"));
    }

    private static async Task CreateLegacySchemaAsync(SqliteConnection connection) => await ExecuteAsync(connection, """
        CREATE TABLE OutboxState (
            OutboxId TEXT PRIMARY KEY,
            BusKey TEXT NULL,
            Status INTEGER NOT NULL,
            NextDeliveryTime TEXT NULL,
            DeliveryAttempts INTEGER NOT NULL,
            LastFailureKind INTEGER NOT NULL,
            LastFailureTime TEXT NULL,
            LastFailure TEXT NULL
        );
        CREATE TABLE OutboxMessage (
            MessageId TEXT PRIMARY KEY,
            OutboxId TEXT NULL,
            MessageType TEXT NOT NULL,
            DestinationAddress TEXT NULL,
            ContentType TEXT NOT NULL,
            Body TEXT NOT NULL,
            CorrelationId TEXT NULL,
            SentTime TEXT NOT NULL,
            EnqueueTime TEXT NULL
        );
        CREATE TABLE InboxState (
            MessageId TEXT NOT NULL,
            ConsumerId TEXT NOT NULL,
            Received TEXT NOT NULL,
            ReceiveCount INTEGER NOT NULL,
            Consumed TEXT NULL,
            PRIMARY KEY (MessageId, ConsumerId)
        );
        """);

    private static string Configure(string script, string storeKey, string legacyMessageType, string contractIdentity) =>
        script
            .Replace("__VICIONE_STORE_KEY__", storeKey, StringComparison.Ordinal)
            .Replace("__LEGACY_MESSAGE_TYPE__", legacyMessageType, StringComparison.Ordinal)
            .Replace("__CONTRACT_IDENTITY__", contractIdentity, StringComparison.Ordinal);

    private static string ReadScript(string fileName)
    {
        Assembly assembly = typeof(ReliableMessagingMigrationTests).Assembly;
        string resourceName = $"ViciOne.ServiceBus.EntityFrameworkCore.Tests.MigrationScripts.{fileName}";
        using Stream stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded migration resource '{resourceName}' was not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static async Task ExecuteAsync(SqliteConnection connection, string sql)
    {
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<long> ScalarInt64Async(SqliteConnection connection, string sql)
    {
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = sql;
        object value = (await command.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;
        return Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static async Task<(long First, long Second)> ReadPairAsync(SqliteConnection connection, string sql)
    {
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = sql;
        await using SqliteDataReader reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        Assert.True(await reader.ReadAsync(TestContext.Current.CancellationToken));
        return (reader.GetInt64(0), reader.GetInt64(1));
    }
}
