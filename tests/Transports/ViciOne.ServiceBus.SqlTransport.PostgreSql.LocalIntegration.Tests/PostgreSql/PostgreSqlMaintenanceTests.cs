using Npgsql;
using ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.PostgreSql;

public sealed class PostgreSqlMaintenanceTests
{
    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0124", "postgresql-native-owner")]
    public async Task RequeueProcedures_MoveOneAndManyUnlockedDeliveriesWithRequestedDelayAndBudgetAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync(
            "requeue-procedures",
            cancellationToken);
        string queueName = fixture.Name("requeue-input");
        await using NpgsqlConnection connection = fixture.CreateConnection();
        await connection.OpenWithinAsync(fixture.OperationTimeout, cancellationToken);
        await CreateQueueAsync(connection, fixture.Schema, queueName, cancellationToken);
        var single = new MaintenanceMessage(Guid.NewGuid(), "single");
        var batch = new MaintenanceMessage(Guid.NewGuid(), "batch");
        await SendAsync(fixture, queueName, [single, batch], cancellationToken);
        Assert.Equal(2, await MoveToErrorQueueAsync(
            connection,
            fixture.Schema,
            queueName,
            [single.Id, batch.Id],
            cancellationToken));
        long singleDeliveryId = await DeliveryIdAsync(connection, fixture.Schema, single.Id, cancellationToken);

        await using (var command = new NpgsqlCommand(
            $"SELECT \"{fixture.Schema}\".requeue_message(@deliveryId, 1, INTERVAL '90 seconds', 4)",
            connection))
        {
            command.Parameters.AddWithValue("deliveryId", singleDeliveryId);
            Assert.Equal(1, Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)));
        }
        await using (var command = new NpgsqlCommand(
            $"SELECT \"{fixture.Schema}\".requeue_messages(@queueName, 2, 1, 10, INTERVAL '0 seconds', 6)",
            connection))
        {
            command.Parameters.AddWithValue("queueName", queueName);
            Assert.Equal(1, Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)));
        }

        RequeueState singleState = await RequeueStateForAsync(connection, fixture.Schema, single.Id, cancellationToken);
        RequeueState batchState = await RequeueStateForAsync(connection, fixture.Schema, batch.Id, cancellationToken);
        Assert.Equal(1, singleState.QueueType);
        Assert.Equal(4, singleState.MaxDeliveryCount);
        Assert.Null(singleState.LockId);
        Assert.Null(singleState.ConsumerId);
        Assert.InRange(
            singleState.EnqueueTimeUtc - singleState.DatabaseNowUtc,
            TimeSpan.FromSeconds(85),
            TimeSpan.FromSeconds(95));
        Assert.Equal(1, batchState.QueueType);
        Assert.Equal(6, batchState.MaxDeliveryCount);
        Assert.Null(batchState.LockId);
        Assert.Null(batchState.ConsumerId);
        Assert.InRange(
            batchState.EnqueueTimeUtc - batchState.DatabaseNowUtc,
            TimeSpan.FromSeconds(-5),
            TimeSpan.FromSeconds(5));
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0125", "postgresql-native-owner")]
    public async Task ProcessMetrics_RollsMinutesIntoHoursAndHoursIntoDaysThenExpiresNinetyDayRowsAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync(
            "metric-rollup",
            cancellationToken);
        string queueName = fixture.Name("metric-input");
        await using NpgsqlConnection connection = fixture.CreateConnection();
        await connection.OpenWithinAsync(fixture.OperationTimeout, cancellationToken);
        long queueId = await CreateQueueAsync(connection, fixture.Schema, queueName, cancellationToken);
        string insert = $$"""
            INSERT INTO "{{fixture.Schema}}".queue_metric
                (start_time, duration, queue_id, consume_count, error_count, dead_letter_count)
            VALUES
                (date_trunc('minute', clock_timestamp() - INTERVAL '9 hours 5 minutes'), INTERVAL '1 minute', @queueId, 11, 12, 13),
                (date_trunc('hour', clock_timestamp() - INTERVAL '49 hours'), INTERVAL '1 hour', @queueId, 21, 22, 23),
                (date_trunc('day', clock_timestamp() - INTERVAL '91 days'), INTERVAL '1 day', @queueId, 31, 32, 33);
            """;
        await using (var command = new NpgsqlCommand(insert, connection))
        {
            command.Parameters.AddWithValue("queueId", queueId);
            Assert.Equal(3, await command.ExecuteNonQueryAsync(cancellationToken));
        }
        await using (var command = new NpgsqlCommand(
            $"SELECT \"{fixture.Schema}\".process_metrics(100)",
            connection))
            Assert.Equal(0, Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)));

        IReadOnlyList<MetricState> metrics = await MetricsForAsync(connection, fixture.Schema, queueId, cancellationToken);
        Assert.Equal(2, metrics.Count);
        MetricState hourly = Assert.Single(metrics, metric => metric.Duration == TimeSpan.FromHours(1));
        Assert.Equal((11L, 12L, 13L), (hourly.ConsumeCount, hourly.ErrorCount, hourly.DeadLetterCount));
        MetricState daily = Assert.Single(metrics, metric => metric.Duration == TimeSpan.FromDays(1));
        Assert.Equal((21L, 22L, 23L), (daily.ConsumeCount, daily.ErrorCount, daily.DeadLetterCount));
    }

    private static async Task<long> CreateQueueAsync(
        NpgsqlConnection connection,
        string schema,
        string queueName,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            $"SELECT \"{schema}\".create_queue_v2(@queueName, NULL, NULL)",
            connection);
        command.Parameters.AddWithValue("queueName", queueName);
        long queueId = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
        Assert.True(queueId > 0);
        return queueId;
    }

    private static async Task SendAsync(
        PostgreSqlTestDatabase fixture,
        string queueName,
        IReadOnlyList<MaintenanceMessage> messages,
        CancellationToken cancellationToken)
    {
        IBusControl sender = SqlBusFactory.Create(fixture.ConfigureHost);
        bool started = false;
        try
        {
            await sender.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            ISendEndpoint endpoint = await sender.GetSendEndpointAsync(new Uri($"queue:{queueName}"), cancellationToken: cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            foreach (MaintenanceMessage message in messages)
            {
                await endpoint.SendAsync(message, context => context.MessageId = message.Id, cancellationToken)
                    .WaitAsync(fixture.OperationTimeout, cancellationToken);
            }
        }
        finally
        {
            if (started)
                await sender.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    private static async Task<int> MoveToErrorQueueAsync(
        NpgsqlConnection connection,
        string schema,
        string queueName,
        IReadOnlyList<Guid> messageIds,
        CancellationToken cancellationToken)
    {
        string sql = $"UPDATE \"{schema}\".message_delivery d SET queue_id = q.id, lock_id = NULL, consumer_id = NULL "
            + $"FROM \"{schema}\".message m, \"{schema}\".queue q "
            + "WHERE m.transport_message_id = d.transport_message_id AND m.message_id = ANY(@messageIds) "
            + "AND q.name = @queueName AND q.type = 2";
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("messageIds", messageIds.ToArray());
        command.Parameters.AddWithValue("queueName", queueName);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<long> DeliveryIdAsync(
        NpgsqlConnection connection,
        string schema,
        Guid messageId,
        CancellationToken cancellationToken)
    {
        string sql = $"SELECT d.message_delivery_id FROM \"{schema}\".message_delivery d "
            + $"JOIN \"{schema}\".message m ON m.transport_message_id = d.transport_message_id "
            + "WHERE m.message_id = @messageId";
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("messageId", messageId);
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static async Task<RequeueState> RequeueStateForAsync(
        NpgsqlConnection connection,
        string schema,
        Guid messageId,
        CancellationToken cancellationToken)
    {
        string sql = $"SELECT q.type, d.enqueue_time, clock_timestamp(), d.max_delivery_count, d.lock_id, d.consumer_id "
            + $"FROM \"{schema}\".message_delivery d "
            + $"JOIN \"{schema}\".message m ON m.transport_message_id = d.transport_message_id "
            + $"JOIN \"{schema}\".queue q ON q.id = d.queue_id WHERE m.message_id = @messageId";
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("messageId", messageId);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        Assert.True(await reader.ReadAsync(cancellationToken));
        var result = new RequeueState(
            reader.GetInt32(0),
            reader.GetDateTime(1),
            reader.GetDateTime(2),
            reader.GetInt32(3),
            reader.IsDBNull(4) ? null : reader.GetGuid(4),
            reader.IsDBNull(5) ? null : reader.GetGuid(5));
        Assert.False(await reader.ReadAsync(cancellationToken));
        return result;
    }

    private static async Task<IReadOnlyList<MetricState>> MetricsForAsync(
        NpgsqlConnection connection,
        string schema,
        long queueId,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            $"SELECT duration, consume_count, error_count, dead_letter_count FROM \"{schema}\".queue_metric "
            + "WHERE queue_id = @queueId ORDER BY duration",
            connection);
        command.Parameters.AddWithValue("queueId", queueId);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<MetricState>();
        while (await reader.ReadAsync(cancellationToken))
            result.Add(new MetricState(reader.GetTimeSpan(0), reader.GetInt64(1), reader.GetInt64(2), reader.GetInt64(3)));
        return result;
    }

    private sealed record MaintenanceMessage(Guid Id, string Value);
    private sealed record RequeueState(
        int QueueType,
        DateTime EnqueueTimeUtc,
        DateTime DatabaseNowUtc,
        int MaxDeliveryCount,
        Guid? LockId,
        Guid? ConsumerId);
    private sealed record MetricState(TimeSpan Duration, long ConsumeCount, long ErrorCount, long DeadLetterCount);
}
