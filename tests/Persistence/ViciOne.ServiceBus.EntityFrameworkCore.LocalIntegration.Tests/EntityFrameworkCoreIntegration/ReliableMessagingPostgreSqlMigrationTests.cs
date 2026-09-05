using System.Reflection;
using Npgsql;
using ViciOne.ServiceBus.EntityFrameworkCore.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCore.LocalIntegration.Tests.EntityFrameworkCoreIntegration;

public sealed class ReliableMessagingPostgreSqlMigrationTests
{
    private const string LegacyMessageType = "urn:message:ViciOne.Tests:OrderAccepted";
    private const string ContractIdentity = "vicione.tests.order-accepted@1";
    private const string StoreKey = "orders-primary";

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-RELIABLE-MIGRATION", "postgresql-preserves-retry-quarantine-inbox-and-capacity-idempotently")]
    public async Task PostgreSqlMigration_PreservesRetainedIntentAndCanResumeIdempotentlyAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase database = await PostgreSqlTestDatabase.CreateAsync(
            "reliable-migration",
            cancellationToken);
        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await ExecuteAsync(connection, """
            CREATE TABLE "OutboxState" (
                "OutboxId" uuid PRIMARY KEY,
                "BusKey" text NULL,
                "Status" integer NOT NULL,
                "NextDeliveryTime" timestamp with time zone NULL,
                "DeliveryAttempts" integer NOT NULL,
                "LastFailureKind" integer NOT NULL,
                "LastFailureTime" timestamp with time zone NULL,
                "LastFailure" text NULL
            );
            CREATE TABLE "OutboxMessage" (
                "MessageId" uuid PRIMARY KEY,
                "OutboxId" uuid NULL,
                "MessageType" text NOT NULL,
                "DestinationAddress" text NULL,
                "ContentType" text NOT NULL,
                "Body" text NOT NULL,
                "CorrelationId" uuid NULL,
                "SentTime" timestamp with time zone NOT NULL,
                "EnqueueTime" timestamp with time zone NULL
            );
            CREATE TABLE "InboxState" (
                "MessageId" uuid NOT NULL,
                "ConsumerId" uuid NOT NULL,
                "Received" timestamp with time zone NOT NULL,
                "ReceiveCount" integer NOT NULL,
                "Consumed" timestamp with time zone NULL,
                PRIMARY KEY ("MessageId", "ConsumerId")
            );
            """, cancellationToken);

        Guid retryId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid quarantineId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        Guid deliveredId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        Guid retryOutboxId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        Guid quarantineOutboxId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        Guid deliveredOutboxId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        Guid consumerId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        await ExecuteAsync(connection, $"""
            INSERT INTO "OutboxState"
                ("OutboxId", "BusKey", "Status", "NextDeliveryTime", "DeliveryAttempts", "LastFailureKind", "LastFailureTime", "LastFailure")
            VALUES
                ('{retryOutboxId}', '{StoreKey}', 1, '2026-09-03T12:05:00Z', 2, 1, '2026-09-03T12:04:00Z', 'network'),
                ('{quarantineOutboxId}', '{StoreKey}', 3, NULL, 4, 3, '2026-09-03T12:06:00Z', 'invariant'),
                ('{deliveredOutboxId}', '{StoreKey}', 2, NULL, 1, 0, NULL, NULL);

            INSERT INTO "OutboxMessage"
                ("MessageId", "OutboxId", "MessageType", "DestinationAddress", "ContentType", "Body", "CorrelationId", "SentTime", "EnqueueTime")
            VALUES
                ('{retryId}', '{retryOutboxId}', '{LegacyMessageType}', 'rabbitmq://orders/input', 'application/json', 'payload1', NULL, '2026-09-03T12:00:00Z', NULL),
                ('{quarantineId}', '{quarantineOutboxId}', '{LegacyMessageType}', 'rabbitmq://orders/input', 'application/json', 'payload2', '{retryId}', '2026-09-03T12:01:00Z', '2026-09-03T12:02:00Z'),
                ('{deliveredId}', '{deliveredOutboxId}', '{LegacyMessageType}', 'rabbitmq://orders/input', 'application/json', 'payload3', NULL, '2026-09-03T12:02:00Z', NULL);

            INSERT INTO "InboxState" ("MessageId", "ConsumerId", "Received", "ReceiveCount", "Consumed")
            VALUES
                ('{retryId}', '{consumerId}', '2026-09-03T11:59:00Z', 3, NULL),
                ('{quarantineId}', '{consumerId}', '2026-09-03T11:58:00Z', 1, '2026-09-03T12:03:00Z');
            """, cancellationToken);

        string script = ReadScript()
            .Replace("__VICIONE_STORE_KEY__", StoreKey, StringComparison.Ordinal)
            .Replace("__LEGACY_MESSAGE_TYPE__", LegacyMessageType, StringComparison.Ordinal)
            .Replace("__CONTRACT_IDENTITY__", ContractIdentity, StringComparison.Ordinal);
        await ExecuteAsync(connection, script, cancellationToken);
        await ExecuteAsync(connection, script, cancellationToken);

        await using (var command = new NpgsqlCommand("""
            SELECT "Id", "ContractIdentity", "DestinationAddress", "Body", "MessageId", "CorrelationId",
                   "StorageSize", "Status", "DueAt", "DeliveryAttempts", "NextAttemptAt",
                   "LastFailureKind", "LastFailureType"
            FROM vicione_outbox ORDER BY "Id";
            """, connection))
        await using (NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            Assert.True(await reader.ReadAsync(cancellationToken));
            Assert.Equal(retryId, reader.GetGuid(0));
            Assert.Equal(ContractIdentity, reader.GetString(1));
            Assert.Equal("rabbitmq://orders/input", reader.GetString(2));
            Assert.Equal("payload1"u8.ToArray(), reader.GetFieldValue<byte[]>(3));
            Assert.Equal(retryId, reader.GetGuid(4));
            Assert.True(reader.IsDBNull(5));
            Assert.Equal(8, reader.GetInt64(6));
            Assert.Equal(1, reader.GetInt32(7));
            Assert.True(reader.IsDBNull(8));
            Assert.Equal(2, reader.GetInt32(9));
            Assert.Equal(DateTimeOffset.Parse("2026-09-03T12:05:00Z"), reader.GetFieldValue<DateTimeOffset>(10));
            Assert.Equal(1, reader.GetInt32(11));
            Assert.True(reader.IsDBNull(12));

            Assert.True(await reader.ReadAsync(cancellationToken));
            Assert.Equal(quarantineId, reader.GetGuid(0));
            Assert.Equal(retryId, reader.GetGuid(5));
            Assert.Equal(2, reader.GetInt32(7));
            Assert.Equal(DateTimeOffset.Parse("2026-09-03T12:02:00Z"), reader.GetFieldValue<DateTimeOffset>(8));
            Assert.Equal(4, reader.GetInt32(9));
            Assert.Equal(5, reader.GetInt32(11));
            Assert.Equal("invariant", reader.GetString(12));
            Assert.False(await reader.ReadAsync(cancellationToken));
        }

        Assert.Equal((2L, 16L), await ReadPairAsync(
            connection,
            $"SELECT \"StoredCount\", \"StoredBytes\" FROM vicione_reliable_capacity WHERE \"StoreKey\" = '{StoreKey}'",
            cancellationToken));
        Assert.Equal((2L, 4L), await ReadPairAsync(
            connection,
            $"SELECT COUNT(*), SUM(\"Attempts\") FROM vicione_inbox WHERE \"StoreKey\" = '{StoreKey}'",
            cancellationToken));
        Assert.Equal(1L, await ScalarAsync(
            connection,
            $"SELECT COUNT(*) FROM vicione_inbox WHERE \"StoreKey\" = '{StoreKey}' AND \"Status\" = 1",
            cancellationToken));
        Assert.Equal(3L, await ScalarAsync(connection, "SELECT COUNT(*) FROM \"OutboxMessage\"", cancellationToken));
    }

    private static string ReadScript()
    {
        Assembly assembly = typeof(ReliableMessagingPostgreSqlMigrationTests).Assembly;
        const string resourceName =
            "ViciOne.ServiceBus.EntityFrameworkCore.LocalIntegration.Tests.MigrationScripts.reliable-messaging-postgresql.sql";
        using Stream stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded migration resource '{resourceName}' was not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static async Task ExecuteAsync(
        NpgsqlConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<long> ScalarAsync(
        NpgsqlConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        object value = (await command.ExecuteScalarAsync(cancellationToken))!;
        return Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static async Task<(long First, long Second)> ReadPairAsync(
        NpgsqlConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        Assert.True(await reader.ReadAsync(cancellationToken));
        return (reader.GetInt64(0), reader.GetInt64(1));
    }
}
