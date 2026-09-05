using System.Data;
using Microsoft.Data.SqlClient;
using ViciOne.ServiceBus.SqlTransport.SqlServer.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.SqlServer.LocalIntegration.Tests.SqlServer;

public sealed class SqlServerDeliveryLimitTests
{
    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0093", "sqlserver-native-owner")]
    public async Task ConfiguredMaxDeliveryCount_IsPersistedInsteadOfTheDatabaseDefaultAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using SqlServerTestDatabase fixture = await SqlServerTestDatabase.CreateAsync(
            "delivery-limit",
            cancellationToken);
        string queueName = fixture.Name("limited-input");

        await DeclareQueueAsync(fixture, queueName, maxDeliveryCount: 3, cancellationToken);
        await using SqlConnection connection = fixture.CreateConnection();
        await connection.OpenWithinAsync(fixture.OperationTimeout, cancellationToken);

        Assert.Equal(3, await QueueMaxDeliveryCountAsync(connection, fixture.Schema, queueName, cancellationToken));
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0095", "sqlserver-native-owner")]
    public async Task ExhaustedDelivery_IsExcludedFromFetchAndMovedByDeadLetterMaintenanceAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using SqlServerTestDatabase fixture = await SqlServerTestDatabase.CreateAsync(
            "exhausted-delivery",
            cancellationToken);
        string queueName = fixture.Name("limited-input");
        await DeclareQueueAsync(fixture, queueName, maxDeliveryCount: 3, cancellationToken);
        var exhausted = new LimitedMessage(Guid.NewGuid(), "exhausted");
        var control = new LimitedMessage(Guid.NewGuid(), "fetchable-control");
        await SendAsync(fixture, queueName, [exhausted, control], cancellationToken);

        await using SqlConnection connection = fixture.CreateConnection();
        await connection.OpenWithinAsync(fixture.OperationTimeout, cancellationToken);
        int changed = await ExhaustDeliveryAsync(connection, fixture.Schema, exhausted.Id, cancellationToken);

        FetchedDelivery[] fetched = await FetchReadyDeliveriesAsync(
            connection,
            fixture.Schema,
            queueName,
            cancellationToken);

        Assert.Equal(1, changed);
        FetchedDelivery fetchedControl = Assert.Single(fetched);
        Assert.Equal(control.Id, fetchedControl.MessageId);
        Assert.DoesNotContain(fetched, item => item.MessageId == exhausted.Id);

        await DeleteFetchedDeliveryAsync(connection, fixture.Schema, fetchedControl, cancellationToken);
        long moved = await DeadLetterExhaustedAsync(connection, fixture.Schema, queueName, cancellationToken);

        Assert.Equal(1, moved);
        Assert.Equal(0, await connection.DeliveryCountAsync(fixture.Schema, queueName, 1, cancellationToken));
        Assert.Equal(1, await connection.DeliveryCountAsync(fixture.Schema, queueName, 3, cancellationToken));
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0130", "sqlserver-native-owner")]
    public async Task FetchProcedures_ProjectTransportTimestampsAsUtcDateTimeOffsetsAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using SqlServerTestDatabase fixture = await SqlServerTestDatabase.CreateAsync(
            "utc-timestamps",
            cancellationToken);
        string normalQueue = fixture.Name("normal-input");
        string partitionedQueue = fixture.Name("partitioned-input");
        await DeclareQueueAsync(fixture, normalQueue, maxDeliveryCount: null, cancellationToken);
        await DeclareQueueAsync(fixture, partitionedQueue, maxDeliveryCount: null, cancellationToken);

        await using SqlConnection connection = fixture.CreateConnection();
        await connection.OpenWithinAsync(fixture.OperationTimeout, cancellationToken);
        DateTimeOffset sentTime = new(2026, 9, 5, 8, 15, 30, TimeSpan.Zero);
        DateTimeOffset expirationTime = sentTime.AddHours(4);

        await InsertDeliveryAsync(connection, fixture.Schema, normalQueue, sentTime, expirationTime, cancellationToken);
        await InsertDeliveryAsync(connection, fixture.Schema, partitionedQueue, sentTime, expirationTime, cancellationToken);

        TransportTimestamps normal = await FetchTransportTimestampsAsync(
            connection,
            fixture.Schema,
            "FetchMessages",
            normalQueue,
            cancellationToken);
        TransportTimestamps partitioned = await FetchTransportTimestampsAsync(
            connection,
            fixture.Schema,
            "FetchMessagesPartitioned",
            partitionedQueue,
            cancellationToken);

        AssertUtcTransportTimestamps(normal, sentTime, expirationTime);
        AssertUtcTransportTimestamps(partitioned, sentTime, expirationTime);
    }

    private static async Task DeclareQueueAsync(
        SqlServerTestDatabase fixture,
        string queueName,
        int? maxDeliveryCount,
        CancellationToken cancellationToken)
    {
        IBusControl bus = SqlBusFactory.Create(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.ConfigureConsumeTopology = false;
                endpoint.MaxDeliveryCount = maxDeliveryCount;
                endpoint.Handler<LimitedMessage>(_ => Task.CompletedTask);
            });
        });
        await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
        await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
    }

    private static async Task SendAsync(
        SqlServerTestDatabase fixture,
        string queueName,
        IReadOnlyList<LimitedMessage> messages,
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
            foreach (LimitedMessage message in messages)
            {
                await endpoint.SendAsync(
                        message,
                        context => context.MessageId = message.Id,
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

    private static async Task<int> QueueMaxDeliveryCountAsync(
        SqlConnection connection,
        string schema,
        string queueName,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(
            $"SELECT MaxDeliveryCount FROM [{schema}].[Queue] WHERE Name = @queueName AND Type = 1",
            connection);
        command.Parameters.AddWithValue("queueName", queueName);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static async Task InsertDeliveryAsync(
        SqlConnection connection,
        string schema,
        string queueName,
        DateTimeOffset sentTime,
        DateTimeOffset expirationTime,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand($"[{schema}].[SendMessageV2]", connection)
        {
            CommandType = CommandType.StoredProcedure,
        };
        command.Parameters.AddWithValue("entityName", queueName);
        command.Parameters.AddWithValue("transportMessageId", Guid.NewGuid());
        command.Parameters.AddWithValue("messageId", Guid.NewGuid());
        command.Parameters.AddWithValue("sentTime", sentTime);
        command.Parameters.AddWithValue("expirationTime", expirationTime);

        Assert.True(Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken)) > 0);
    }

    private static async Task<TransportTimestamps> FetchTransportTimestampsAsync(
        SqlConnection connection,
        string schema,
        string procedure,
        string queueName,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand($"[{schema}].[{procedure}]", connection)
        {
            CommandType = CommandType.StoredProcedure,
        };
        command.Parameters.AddWithValue("queueName", queueName);
        command.Parameters.AddWithValue("consumerId", Guid.NewGuid());
        command.Parameters.AddWithValue("lockId", Guid.NewGuid());
        command.Parameters.AddWithValue("lockDuration", 60);
        command.Parameters.AddWithValue("fetchCount", 1);
        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        Assert.True(await reader.ReadAsync(cancellationToken));

        int enqueueOrdinal = reader.GetOrdinal("EnqueueTime");
        int expirationOrdinal = reader.GetOrdinal("ExpirationTime");
        int sentOrdinal = reader.GetOrdinal("SentTime");
        Assert.Equal(typeof(DateTimeOffset), reader.GetFieldType(enqueueOrdinal));
        Assert.Equal(typeof(DateTimeOffset), reader.GetFieldType(expirationOrdinal));
        Assert.Equal(typeof(DateTimeOffset), reader.GetFieldType(sentOrdinal));

        return new TransportTimestamps(
            reader.GetFieldValue<DateTimeOffset>(enqueueOrdinal),
            reader.GetFieldValue<DateTimeOffset>(expirationOrdinal),
            reader.GetFieldValue<DateTimeOffset>(sentOrdinal));
    }

    private static void AssertUtcTransportTimestamps(
        TransportTimestamps actual,
        DateTimeOffset expectedSentTime,
        DateTimeOffset expectedExpirationTime)
    {
        Assert.Equal(TimeSpan.Zero, actual.EnqueueTime.Offset);
        Assert.Equal(TimeSpan.Zero, actual.ExpirationTime.Offset);
        Assert.Equal(TimeSpan.Zero, actual.SentTime.Offset);
        Assert.Equal(expectedExpirationTime, actual.ExpirationTime);
        Assert.Equal(expectedSentTime, actual.SentTime);
    }

    private static async Task<int> ExhaustDeliveryAsync(
        SqlConnection connection,
        string schema,
        Guid messageId,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(
            $"UPDATE d SET d.DeliveryCount = d.MaxDeliveryCount FROM [{schema}].[MessageDelivery] d "
            + $"INNER JOIN [{schema}].[Message] m ON m.TransportMessageId = d.TransportMessageId "
            + "WHERE m.MessageId = @messageId",
            connection);
        command.Parameters.AddWithValue("messageId", messageId);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<FetchedDelivery[]> FetchReadyDeliveriesAsync(
        SqlConnection connection,
        string schema,
        string queueName,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand($"[{schema}].[FetchMessages]", connection)
        {
            CommandType = CommandType.StoredProcedure,
        };
        command.Parameters.AddWithValue("queueName", queueName);
        command.Parameters.AddWithValue("consumerId", Guid.NewGuid());
        command.Parameters.AddWithValue("lockId", Guid.NewGuid());
        command.Parameters.AddWithValue("lockDuration", 60);
        command.Parameters.AddWithValue("fetchCount", 10);
        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<FetchedDelivery>();
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new FetchedDelivery(
                reader.GetGuid(reader.GetOrdinal("MessageId")),
                reader.GetInt64(reader.GetOrdinal("MessageDeliveryId")),
                reader.GetGuid(reader.GetOrdinal("LockId"))));
        }
        return result.ToArray();
    }

    private static async Task DeleteFetchedDeliveryAsync(
        SqlConnection connection,
        string schema,
        FetchedDelivery delivery,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand($"[{schema}].[DeleteMessage]", connection)
        {
            CommandType = CommandType.StoredProcedure,
        };
        command.Parameters.AddWithValue("messageDeliveryId", delivery.DeliveryId);
        command.Parameters.AddWithValue("lockId", delivery.LockId);
        Assert.Equal(delivery.DeliveryId, Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken)));
    }

    private static async Task<long> DeadLetterExhaustedAsync(
        SqlConnection connection,
        string schema,
        string queueName,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand($"[{schema}].[DeadLetterMessages]", connection)
        {
            CommandType = CommandType.StoredProcedure,
        };
        command.Parameters.AddWithValue("queueName", queueName);
        command.Parameters.AddWithValue("messageCount", 100);
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
    }

    private sealed record LimitedMessage(Guid Id, string Value);
    private sealed record FetchedDelivery(Guid MessageId, long DeliveryId, Guid LockId);
    private sealed record TransportTimestamps(
        DateTimeOffset EnqueueTime,
        DateTimeOffset ExpirationTime,
        DateTimeOffset SentTime);
}
