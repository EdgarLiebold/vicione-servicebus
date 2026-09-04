using System.Collections.Concurrent;
using Npgsql;
using ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.PostgreSql;

public sealed class PostgreSqlDeliveryStateTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("OBL-R0-SQL-0110", "postgresql-native-owner")]
    public async Task ExpiredDelivery_IsDeletedOrDeadLetteredWithoutEnteringTheConsumer(bool deadLetterExpiredMessages)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync(
            deadLetterExpiredMessages ? "expired-deadletter" : "expired-delete",
            cancellationToken);
        string queueName = fixture.Name("expired-input");
        await DeclareQueue(fixture, queueName, cancellationToken);
        var message = new StateMessage(Guid.NewGuid(), "expired");
        await Send(fixture, queueName, message, cancellationToken);
        await using (NpgsqlConnection arrange = fixture.CreateConnection())
        {
            await arrange.OpenWithin(fixture.OperationTimeout, cancellationToken);
            Assert.Equal(1, await ExpireDelivery(arrange, fixture.Schema, message.Id, cancellationToken));
        }
        var control = new StateMessage(Guid.NewGuid(), "control");
        await Send(fixture, queueName, control, cancellationToken);

        var consumed = new ConcurrentQueue<Guid>();
        var controlDelivered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        IBusControl receiver = SqlBusFactory.Create(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.ConfigureConsumeTopology = false;
                endpoint.PrefetchCount = 1;
                endpoint.ConcurrentMessageLimit = 1;
                endpoint.DeadLetterExpiredMessages = deadLetterExpiredMessages;
                endpoint.Handler<StateMessage>(context =>
                {
                    consumed.Enqueue(context.Message.Id);
                    if (context.Message.Id == control.Id)
                        controlDelivered.TrySetResult();
                    return Task.CompletedTask;
                });
            });
        });
        bool started = false;
        try
        {
            await receiver.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            await controlDelivered.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
        finally
        {
            if (started)
                await receiver.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }

        await using NpgsqlConnection connection = fixture.CreateConnection();
        await connection.OpenWithin(fixture.OperationTimeout, cancellationToken);
        Assert.Equal([control.Id], consumed);
        Assert.Equal(0, await connection.DeliveryCount(fixture.Schema, queueName, 1, cancellationToken));
        Assert.Equal(deadLetterExpiredMessages ? 1 : 0,
            await connection.DeliveryCount(fixture.Schema, queueName, 3, cancellationToken));
        Assert.Equal(deadLetterExpiredMessages ? 1 : 0,
            await connection.MessageCount(fixture.Schema, message.Id, cancellationToken));
        if (deadLetterExpiredMessages)
        {
            string? headers = await connection.TransportHeadersForMessage(
                fixture.Schema,
                message.Id,
                cancellationToken);
            Assert.Contains("expired", Assert.IsType<string>(headers), StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0114", "postgresql-native-owner")]
    public async Task PurgeQueue_RemovesDeliveriesAndOrphanedMessagesAcrossAllThreeQueueTypes()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync(
            "purge-all-types",
            cancellationToken);
        string queueName = fixture.Name("purge-input");
        await DeclareQueue(fixture, queueName, cancellationToken);
        StateMessage[] messages =
        [
            new(Guid.NewGuid(), "queue"),
            new(Guid.NewGuid(), "error"),
            new(Guid.NewGuid(), "dead-letter"),
        ];
        foreach (StateMessage message in messages)
            await Send(fixture, queueName, message, cancellationToken);

        await using NpgsqlConnection connection = fixture.CreateConnection();
        await connection.OpenWithin(fixture.OperationTimeout, cancellationToken);
        Assert.Equal(1, await MoveDeliveryToQueueType(
            connection,
            fixture.Schema,
            queueName,
            messages[1].Id,
            queueType: 2,
            cancellationToken));
        Assert.Equal(1, await MoveDeliveryToQueueType(
            connection,
            fixture.Schema,
            queueName,
            messages[2].Id,
            queueType: 3,
            cancellationToken));
        Assert.Equal(1, await connection.DeliveryCount(fixture.Schema, queueName, 1, cancellationToken));
        Assert.Equal(1, await connection.DeliveryCount(fixture.Schema, queueName, 2, cancellationToken));
        Assert.Equal(1, await connection.DeliveryCount(fixture.Schema, queueName, 3, cancellationToken));

        await Purge(connection, fixture.Schema, queueName, cancellationToken);

        Assert.Equal(0, await connection.DeliveryCount(fixture.Schema, queueName, 1, cancellationToken));
        Assert.Equal(0, await connection.DeliveryCount(fixture.Schema, queueName, 2, cancellationToken));
        Assert.Equal(0, await connection.DeliveryCount(fixture.Schema, queueName, 3, cancellationToken));
        foreach (StateMessage message in messages)
            Assert.Equal(0, await connection.MessageCount(fixture.Schema, message.Id, cancellationToken));
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0135", "postgresql-native-owner")]
    public async Task DeleteScheduledMessage_DeletesOnlyAnUndeliveredUnlockedDelivery()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync(
            "scheduled-states",
            cancellationToken);
        string queueName = fixture.Name("scheduled-input");
        await DeclareQueue(fixture, queueName, cancellationToken);
        var cancellable = new ScheduledState(Guid.NewGuid(), Guid.NewGuid(), "ready");
        var delivered = new ScheduledState(Guid.NewGuid(), Guid.NewGuid(), "delivered");
        var locked = new ScheduledState(Guid.NewGuid(), Guid.NewGuid(), "locked");
        await SendScheduled(fixture, queueName, [cancellable, delivered, locked], cancellationToken);

        await using NpgsqlConnection connection = fixture.CreateConnection();
        await connection.OpenWithin(fixture.OperationTimeout, cancellationToken);
        Assert.Equal(1, await SetDeliveryState(
            connection,
            fixture.Schema,
            delivered.MessageId,
            deliveryCount: 1,
            lockId: null,
            cancellationToken));
        Assert.Equal(1, await SetDeliveryState(
            connection,
            fixture.Schema,
            locked.MessageId,
            deliveryCount: 0,
            lockId: Guid.NewGuid(),
            cancellationToken));

        Assert.Equal(1, await DeleteScheduled(connection, fixture.Schema, cancellable.TokenId, cancellationToken));
        Assert.Equal(0, await DeleteScheduled(connection, fixture.Schema, delivered.TokenId, cancellationToken));
        Assert.Equal(0, await DeleteScheduled(connection, fixture.Schema, locked.TokenId, cancellationToken));
        Assert.Equal(0, await connection.MessageCount(fixture.Schema, cancellable.MessageId, cancellationToken));
        Assert.Equal(1, await connection.MessageCount(fixture.Schema, delivered.MessageId, cancellationToken));
        Assert.Equal(1, await connection.MessageCount(fixture.Schema, locked.MessageId, cancellationToken));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("OBL-R0-SQL-0133", "postgresql-native-owner")]
    public async Task ErrorQueueMove_ResetsExistingExpirationToFourteenDaysAndKeepsMissingExpirationNull(bool hasTimeToLive)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync(
            hasTimeToLive ? "error-ttl" : "error-no-ttl",
            cancellationToken);
        string queueName = fixture.Name("fault-input");
        string faultQueueName = fixture.Name("fault-observer");
        var faulted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        IBusControl bus = SqlBusFactory.Create(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
                endpoint.Handler<ErrorMessage>(_ => throw new DeliberateStateException()));
            configurator.ReceiveEndpoint(faultQueueName, endpoint =>
                endpoint.Handler<Fault<ErrorMessage>>(_ =>
                {
                    faulted.TrySetResult();
                    return Task.CompletedTask;
                }));
        });
        bool started = false;
        var message = new ErrorMessage(Guid.NewGuid());
        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            ISendEndpoint endpoint = await bus.GetSendEndpoint(new Uri($"queue:{queueName}"))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await endpoint.Send(
                    message,
                    context =>
                    {
                        context.MessageId = message.Id;
                        if (hasTimeToLive)
                            context.TimeToLive = TimeSpan.FromMinutes(30);
                    },
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await faulted.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }

        await using NpgsqlConnection connection = fixture.CreateConnection();
        await connection.OpenWithin(fixture.OperationTimeout, cancellationToken);
        ErrorExpiration stored = await ErrorExpirationForMessage(
            connection,
            fixture.Schema,
            message.Id,
            cancellationToken);
        Assert.Equal(2, stored.QueueType);
        if (hasTimeToLive)
        {
            DateTime expiration = Assert.IsType<DateTime>(stored.ExpirationTimeUtc);
            Assert.InRange(
                expiration - stored.DatabaseNowUtc,
                TimeSpan.FromDays(14) - TimeSpan.FromMinutes(2),
                TimeSpan.FromDays(14) + TimeSpan.FromMinutes(2));
        }
        else
            Assert.Null(stored.ExpirationTimeUtc);
    }

    private static async Task DeclareQueue(
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
                endpoint.Handler<StateMessage>(_ => Task.CompletedTask);
                endpoint.Handler<ScheduledState>(_ => Task.CompletedTask);
            });
        });
        await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
        await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
    }

    private static async Task Send(
        PostgreSqlTestDatabase fixture,
        string queueName,
        StateMessage message,
        CancellationToken cancellationToken)
    {
        IBusControl bus = SqlBusFactory.Create(fixture.ConfigureHost);
        bool started = false;
        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            ISendEndpoint endpoint = await bus.GetSendEndpoint(new Uri($"queue:{queueName}"))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await endpoint.Send(
                    message,
                    context =>
                    {
                        context.MessageId = message.Id;
                        context.TimeToLive = TimeSpan.FromHours(1);
                    },
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    private static async Task SendScheduled(
        PostgreSqlTestDatabase fixture,
        string queueName,
        IReadOnlyList<ScheduledState> messages,
        CancellationToken cancellationToken)
    {
        IBusControl bus = SqlBusFactory.Create(fixture.ConfigureHost);
        bool started = false;
        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            ISendEndpoint endpoint = await bus.GetSendEndpoint(new Uri($"queue:{queueName}"))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            foreach (ScheduledState message in messages)
            {
                await endpoint.Send(
                        message,
                        context =>
                        {
                            context.MessageId = message.MessageId;
                            context.Delay = TimeSpan.FromHours(1);
                            context.ScheduledMessageId = message.TokenId;
                            context.Headers.Set(MessageHeaders.SchedulingTokenId, message.TokenId.ToString("D"));
                        },
                        cancellationToken)
                    .WaitAsync(fixture.OperationTimeout, cancellationToken);
            }
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    private static async Task<int> ExpireDelivery(
        NpgsqlConnection connection,
        string schema,
        Guid messageId,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            $"UPDATE \"{schema}\".message_delivery d SET expiration_time = clock_timestamp() - INTERVAL '1 minute' "
            + $"FROM \"{schema}\".message m "
            + "WHERE m.transport_message_id = d.transport_message_id AND m.message_id = @messageId",
            connection);
        command.Parameters.AddWithValue("messageId", messageId);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<int> MoveDeliveryToQueueType(
        NpgsqlConnection connection,
        string schema,
        string queueName,
        Guid messageId,
        int queueType,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            $"UPDATE \"{schema}\".message_delivery d SET queue_id = q.id "
            + $"FROM \"{schema}\".message m, \"{schema}\".queue q "
            + "WHERE m.transport_message_id = d.transport_message_id AND m.message_id = @messageId "
            + "AND q.name = @queueName AND q.type = @queueType",
            connection);
        command.Parameters.AddWithValue("messageId", messageId);
        command.Parameters.AddWithValue("queueName", queueName);
        command.Parameters.AddWithValue("queueType", queueType);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task Purge(
        NpgsqlConnection connection,
        string schema,
        string queueName,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand($"SELECT \"{schema}\".purge_queue(@queueName)", connection);
        command.Parameters.AddWithValue("queueName", queueName);
        await command.ExecuteScalarAsync(cancellationToken);
    }

    private static async Task<int> SetDeliveryState(
        NpgsqlConnection connection,
        string schema,
        Guid messageId,
        int deliveryCount,
        Guid? lockId,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            $"UPDATE \"{schema}\".message_delivery d SET delivery_count = @deliveryCount, lock_id = @lockId "
            + $"FROM \"{schema}\".message m "
            + "WHERE m.transport_message_id = d.transport_message_id AND m.message_id = @messageId",
            connection);
        command.Parameters.AddWithValue("deliveryCount", deliveryCount);
        command.Parameters.AddWithValue("lockId", (object?)lockId ?? DBNull.Value);
        command.Parameters.AddWithValue("messageId", messageId);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<long> DeleteScheduled(
        NpgsqlConnection connection,
        string schema,
        Guid tokenId,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            $"SELECT COUNT(*) FROM \"{schema}\".delete_scheduled_message(@tokenId)",
            connection);
        command.Parameters.AddWithValue("tokenId", tokenId);
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static async Task<ErrorExpiration> ErrorExpirationForMessage(
        NpgsqlConnection connection,
        string schema,
        Guid messageId,
        CancellationToken cancellationToken)
    {
        string commandText = $"SELECT q.type, d.expiration_time, clock_timestamp() "
            + $"FROM \"{schema}\".message_delivery d "
            + $"JOIN \"{schema}\".message m ON m.transport_message_id = d.transport_message_id "
            + $"JOIN \"{schema}\".queue q ON q.id = d.queue_id "
            + "WHERE m.message_id = @messageId";
        await using var command = new NpgsqlCommand(commandText, connection);
        command.Parameters.AddWithValue("messageId", messageId);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        Assert.True(await reader.ReadAsync(cancellationToken), "The faulted delivery is missing from PostgreSQL.");
        var result = new ErrorExpiration(
            reader.GetInt32(0),
            reader.IsDBNull(1) ? null : reader.GetDateTime(1),
            reader.GetDateTime(2));
        Assert.False(await reader.ReadAsync(cancellationToken), "The faulted message has more than one delivery.");
        return result;
    }

    private sealed record StateMessage(Guid Id, string Value);
    private sealed record ScheduledState(Guid MessageId, Guid TokenId, string Value);
    private sealed record ErrorMessage(Guid Id);
    private sealed record ErrorExpiration(int QueueType, DateTime? ExpirationTimeUtc, DateTime DatabaseNowUtc);

    private sealed class DeliberateStateException : Exception
    {
    }
}
