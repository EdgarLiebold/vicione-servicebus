using System.Text.Json;
using Npgsql;
using ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.PostgreSql;

public sealed class PostgreSqlUnlockTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("OBL-R0-SQL-0111", "postgresql-native-owner")]
    public async Task FaultUnlock_UsesConfiguredOrZeroDelayAndPersistsAllFaultHeadersAsync(bool configureDelay)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync(
            configureDelay ? "fault-delay" : "fault-no-delay",
            cancellationToken);
        string queueName = fixture.Name("fault-input");
        TimeSpan expectedDelay = configureDelay ? TimeSpan.FromSeconds(90) : TimeSpan.Zero;
        await CreateUnlockAuditAsync(fixture, cancellationToken);

        var observer = new FaultingReceiveObserver();
        IBusControl bus = SqlBusFactory.Create(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                if (configureDelay)
                    endpoint.UnlockDelay = expectedDelay;
                endpoint.Handler<UnlockMessage>(_ => Task.CompletedTask);
            });
        });
        using ConnectHandle observerHandle = bus.ConnectReceiveObserver(observer);
        bool started = false;
        var message = new UnlockMessage(Guid.NewGuid());
        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            ISendEndpoint endpoint = await bus.GetSendEndpointAsync(new Uri($"queue:{queueName}"), TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            await endpoint.SendAsync(message, context => context.MessageId = message.Id, cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await observer.Faulted.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }

        await using NpgsqlConnection connection = fixture.CreateConnection();
        await connection.OpenWithinAsync(fixture.OperationTimeout, cancellationToken);
        UnlockAudit audit = await ReadUnlockAuditAsync(connection, fixture.Schema, message.Id, cancellationToken);
        Assert.Null(audit.LockId);
        Assert.Null(audit.ConsumerId);
        TimeSpan observedDelay = audit.EnqueueTimeUtc - audit.RecordedAtUtc;
        Assert.InRange(
            observedDelay,
            expectedDelay - TimeSpan.FromSeconds(5),
            expectedDelay + TimeSpan.FromSeconds(5));
        using JsonDocument headers = JsonDocument.Parse(audit.TransportHeaders);
        string serializedHeaders = headers.RootElement.GetRawText();
        Assert.Contains(MessageHeaders.Reason, serializedHeaders, StringComparison.Ordinal);
        Assert.Contains(MessageHeaders.FaultExceptionType, serializedHeaders, StringComparison.Ordinal);
        Assert.Contains(MessageHeaders.FaultMessage, serializedHeaders, StringComparison.Ordinal);
        Assert.Contains(MessageHeaders.FaultStackTrace, serializedHeaders, StringComparison.Ordinal);
        Assert.Contains("fault-unlock-probe", serializedHeaders, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("OBL-R0-SQL-0120", "postgresql-native-owner")]
    public async Task UnlockProcedure_ClearsBothLockOwnersWithAndWithoutDelayAsync(bool useDelay)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync(
            useDelay ? "unlock-delay" : "unlock-now",
            cancellationToken);
        string queueName = fixture.Name("unlock-input");
        await DeclareQueueAsync(fixture, queueName, cancellationToken);
        var message = new UnlockMessage(Guid.NewGuid());
        await SendAsync(fixture, queueName, message, cancellationToken);

        await using NpgsqlConnection connection = fixture.CreateConnection();
        await connection.OpenWithinAsync(fixture.OperationTimeout, cancellationToken);
        var lockId = Guid.NewGuid();
        var consumerId = Guid.NewGuid();
        long deliveryId = await LockDeliveryAsync(
            connection,
            fixture.Schema,
            message.Id,
            lockId,
            consumerId,
            cancellationToken);
        TimeSpan delay = useDelay ? TimeSpan.FromSeconds(90) : TimeSpan.Zero;

        await using (var command = new NpgsqlCommand(
            $"SELECT \"{fixture.Schema}\".unlock_message(@deliveryId, @lockId, @delay, @headers::jsonb)",
            connection))
        {
            command.Parameters.AddWithValue("deliveryId", deliveryId);
            command.Parameters.AddWithValue("lockId", lockId);
            command.Parameters.AddWithValue("delay", delay);
            command.Parameters.AddWithValue("headers", "{\"probe\":\"unlock\"}");
            Assert.Equal(deliveryId, Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken)));
        }

        UnlockState state = await ReadUnlockStateAsync(connection, fixture.Schema, message.Id, cancellationToken);
        Assert.Null(state.LockId);
        Assert.Null(state.ConsumerId);
        Assert.InRange(
            state.EnqueueTimeUtc - state.DatabaseNowUtc,
            delay - TimeSpan.FromSeconds(5),
            delay + TimeSpan.FromSeconds(5));
        using JsonDocument headers = JsonDocument.Parse(state.TransportHeaders);
        Assert.Equal("unlock", headers.RootElement.GetProperty("probe").GetString());
    }

    private static async Task CreateUnlockAuditAsync(PostgreSqlTestDatabase fixture, CancellationToken cancellationToken)
    {
        string sql = $$"""
            CREATE TABLE "{{fixture.Schema}}".unlock_audit
            (
                transport_message_id uuid NOT NULL,
                message_id uuid NOT NULL,
                enqueue_time timestamptz NOT NULL,
                recorded_at timestamptz NOT NULL,
                lock_id uuid NULL,
                consumer_id uuid NULL,
                transport_headers jsonb NOT NULL
            );
            CREATE FUNCTION "{{fixture.Schema}}".capture_unlock() RETURNS trigger AS
            $body$
            BEGIN
                IF OLD.consumer_id IS NOT NULL AND NEW.consumer_id IS NULL AND OLD.lock_id IS NOT NULL THEN
                    INSERT INTO "{{fixture.Schema}}".unlock_audit
                        (transport_message_id, message_id, enqueue_time, recorded_at, lock_id, consumer_id, transport_headers)
                    VALUES
                        (NEW.transport_message_id,
                         (SELECT message_id FROM "{{fixture.Schema}}".message WHERE transport_message_id = NEW.transport_message_id),
                         NEW.enqueue_time, clock_timestamp(), NEW.lock_id, NEW.consumer_id,
                         COALESCE(NEW.transport_headers, '{}'::jsonb));
                END IF;
                RETURN NEW;
            END;
            $body$ LANGUAGE plpgsql;
            CREATE TRIGGER capture_unlock
                AFTER UPDATE ON "{{fixture.Schema}}".message_delivery
                FOR EACH ROW EXECUTE FUNCTION "{{fixture.Schema}}".capture_unlock();
            """;
        await using NpgsqlConnection connection = fixture.CreateConnection();
        await connection.OpenWithinAsync(fixture.OperationTimeout, cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<UnlockAudit> ReadUnlockAuditAsync(
        NpgsqlConnection connection,
        string schema,
        Guid messageId,
        CancellationToken cancellationToken)
    {
        string sql = $"SELECT a.enqueue_time, a.recorded_at, a.lock_id, a.consumer_id, a.transport_headers::text "
            + $"FROM \"{schema}\".unlock_audit a WHERE a.message_id = @messageId ORDER BY a.recorded_at";
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("messageId", messageId);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        Assert.True(await reader.ReadAsync(cancellationToken), "The fault path did not execute the PostgreSQL unlock update.");
        var result = new UnlockAudit(
            reader.GetDateTime(0),
            reader.GetDateTime(1),
            reader.IsDBNull(2) ? null : reader.GetGuid(2),
            reader.IsDBNull(3) ? null : reader.GetGuid(3),
            reader.GetString(4));
        Assert.False(await reader.ReadAsync(cancellationToken), "The message was unlocked more than once.");
        return result;
    }

    private static async Task<long> LockDeliveryAsync(
        NpgsqlConnection connection,
        string schema,
        Guid messageId,
        Guid lockId,
        Guid consumerId,
        CancellationToken cancellationToken)
    {
        string sql = $"UPDATE \"{schema}\".message_delivery d "
            + "SET lock_id = @lockId, consumer_id = @consumerId "
            + $"FROM \"{schema}\".message m "
            + "WHERE m.transport_message_id = d.transport_message_id AND m.message_id = @messageId "
            + "RETURNING d.message_delivery_id";
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("lockId", lockId);
        command.Parameters.AddWithValue("consumerId", consumerId);
        command.Parameters.AddWithValue("messageId", messageId);
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static async Task<UnlockState> ReadUnlockStateAsync(
        NpgsqlConnection connection,
        string schema,
        Guid messageId,
        CancellationToken cancellationToken)
    {
        string sql = $"SELECT d.enqueue_time, clock_timestamp(), d.lock_id, d.consumer_id, d.transport_headers::text "
            + $"FROM \"{schema}\".message_delivery d JOIN \"{schema}\".message m "
            + "ON m.transport_message_id = d.transport_message_id WHERE m.message_id = @messageId";
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("messageId", messageId);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        Assert.True(await reader.ReadAsync(cancellationToken));
        var result = new UnlockState(
            reader.GetDateTime(0),
            reader.GetDateTime(1),
            reader.IsDBNull(2) ? null : reader.GetGuid(2),
            reader.IsDBNull(3) ? null : reader.GetGuid(3),
            reader.GetString(4));
        Assert.False(await reader.ReadAsync(cancellationToken));
        return result;
    }

    private static async Task DeclareQueueAsync(
        PostgreSqlTestDatabase fixture,
        string queueName,
        CancellationToken cancellationToken)
    {
        IBusControl bus = SqlBusFactory.Create(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.ConfigureConsumeTopology = false;
                endpoint.Handler<UnlockMessage>(_ => Task.CompletedTask);
            });
        });
        await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
        await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
    }

    private static async Task SendAsync(
        PostgreSqlTestDatabase fixture,
        string queueName,
        UnlockMessage message,
        CancellationToken cancellationToken)
    {
        IBusControl bus = SqlBusFactory.Create(fixture.ConfigureHost);
        bool started = false;
        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            ISendEndpoint endpoint = await bus.GetSendEndpointAsync(new Uri($"queue:{queueName}"), cancellationToken: cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await endpoint.SendAsync(message, context => context.MessageId = message.Id, cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    private sealed record UnlockMessage(Guid Id);
    private sealed record UnlockAudit(
        DateTime EnqueueTimeUtc,
        DateTime RecordedAtUtc,
        Guid? LockId,
        Guid? ConsumerId,
        string TransportHeaders);
    private sealed record UnlockState(
        DateTime EnqueueTimeUtc,
        DateTime DatabaseNowUtc,
        Guid? LockId,
        Guid? ConsumerId,
        string TransportHeaders);

    private sealed class FaultingReceiveObserver : IReceiveObserver
    {
        private int _preReceiveCount;

        public TaskCompletionSource Faulted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task PreReceiveAsync(ReceiveContext context)
        {
            if (Interlocked.Increment(ref _preReceiveCount) == 1)
                throw new DeliberateUnlockException("fault-unlock-probe");
            return Task.CompletedTask;
        }

        public Task PostReceiveAsync(ReceiveContext context) => Task.CompletedTask;

        public Task PostConsumeAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType)
            where T : class => Task.CompletedTask;

        public Task ConsumeFaultAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception)
            where T : class => Task.CompletedTask;

        public Task ReceiveFaultAsync(ReceiveContext context, Exception exception)
        {
            Faulted.TrySetResult();
            return Task.CompletedTask;
        }
    }

    private sealed class DeliberateUnlockException(string message) : Exception(message);
}
