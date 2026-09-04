using System.Data;
using Microsoft.Data.SqlClient;
using ViciOne.ServiceBus.SqlTransport.SqlServer.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.SqlServer.LocalIntegration.Tests.SqlServer;

public sealed class SqlServerMaintenanceAndTopologyTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("OBL-R0-SQL-0117", "sqlserver-native-owner")]
    public async Task MaintenanceExecutesOrphanCleanupForEmptyAndPopulatedBatchesAsync(bool insertOrphan)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using SqlServerTestDatabase fixture = await SqlServerTestDatabase.CreateAsync(
            "orphan-maintenance",
            cancellationToken);
        Guid orphanId = Guid.NewGuid();
        await using SqlConnection inspection = fixture.CreateConnection();
        await inspection.OpenWithinAsync(fixture.OperationTimeout, cancellationToken);
        if (insertOrphan)
        {
            await using var insert = new SqlCommand(
                $"INSERT INTO [{fixture.Schema}].[Message] (TransportMessageId) VALUES (@id)",
                inspection);
            insert.Parameters.AddWithValue("id", orphanId);
            Assert.Equal(1, await insert.ExecuteNonQueryAsync(cancellationToken));
        }

        IBusControl bus = SqlBusFactory.Create(configurator =>
            configurator.UseSqlServer(fixture.ConnectionString, host =>
            {
                host.Schema = fixture.Schema;
                host.MaintenanceInterval = TimeSpan.FromMilliseconds(50);
                host.QueueCleanupInterval = TimeSpan.FromMilliseconds(50);
                host.MaintenanceBatchSize = 1;
            }));
        bool started = false;
        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            await WaitForProcedureExecutionAsync(inspection, fixture, "RemoveOrphanedMessages", cancellationToken);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }

        Assert.Equal(0, await OrphanCountAsync(inspection, fixture, orphanId, cancellationToken));
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 2)]
    [RequirementCoverage("OBL-R0-SQL-0118", "sqlserver-native-owner")]
    public async Task DeadLetterPassReportsFewerThanAndExactlyTheRequestedBatchAsync(int exhaustedCount, int batchSize)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using SqlServerTestDatabase fixture = await SqlServerTestDatabase.CreateAsync(
            "dead-letter-count",
            cancellationToken);
        await using SqlConnection connection = fixture.CreateConnection();
        await connection.OpenWithinAsync(fixture.OperationTimeout, cancellationToken);
        string queueName = fixture.Name("input");
        await CreateQueueAsync(connection, fixture.Schema, queueName, cancellationToken);
        for (int index = 0; index < exhaustedCount; ++index)
        {
            long deliveryId = await SendAsync(connection, fixture.Schema, queueName, Guid.NewGuid(), cancellationToken);
            await ExhaustAsync(connection, fixture.Schema, deliveryId, cancellationToken);
        }

        long moved = await DeadLetterAsync(connection, fixture.Schema, queueName, batchSize, cancellationToken);

        Assert.Equal(exhaustedCount, moved);
        Assert.Equal(exhaustedCount, await connection.DeliveryCountAsync(fixture.Schema, queueName, 3, cancellationToken));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [RequirementCoverage("OBL-R0-SQL-0119", "sqlserver-native-owner")]
    public async Task DeadLetterMetricIsAbsentForNoOpAndExactForMovedRowsAsync(int exhaustedCount)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using SqlServerTestDatabase fixture = await SqlServerTestDatabase.CreateAsync(
            "dead-letter-metric",
            cancellationToken);
        await using SqlConnection connection = fixture.CreateConnection();
        await connection.OpenWithinAsync(fixture.OperationTimeout, cancellationToken);
        string queueName = fixture.Name("input");
        long queueId = await CreateQueueAsync(connection, fixture.Schema, queueName, cancellationToken);
        if (exhaustedCount == 1)
        {
            long deliveryId = await SendAsync(connection, fixture.Schema, queueName, Guid.NewGuid(), cancellationToken);
            await ExhaustAsync(connection, fixture.Schema, deliveryId, cancellationToken);
        }

        Assert.Equal(exhaustedCount, await DeadLetterAsync(connection, fixture.Schema, queueName, 10, cancellationToken));
        (int rows, long count) = await DeadLetterMetricAsync(connection, fixture.Schema, queueId, cancellationToken);

        Assert.Equal(exhaustedCount, rows);
        Assert.Equal(exhaustedCount, count);
    }

    [Theory]
    [InlineData("SendMessageV2")]
    [InlineData("PublishMessageV2")]
    [InlineData("DeleteMessage")]
    [InlineData("TouchQueue")]
    [InlineData("DeadLetterMessages")]
    [RequirementCoverage("OBL-R0-SQL-0121", "sqlserver-native-owner")]
    public async Task ClientFacingStoredProcedureReportsItsExactOutcomeAsync(string procedure)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using SqlServerTestDatabase fixture = await SqlServerTestDatabase.CreateAsync(
            "procedure-outcome",
            cancellationToken);
        await using SqlConnection connection = fixture.CreateConnection();
        await connection.OpenWithinAsync(fixture.OperationTimeout, cancellationToken);
        string entityName = fixture.Name(procedure);

        switch (procedure)
        {
            case "SendMessageV2":
                {
                    await CreateQueueAsync(connection, fixture.Schema, entityName, cancellationToken);
                    long deliveryId = await SendAsync(connection, fixture.Schema, entityName, Guid.NewGuid(), cancellationToken);
                    Assert.True(deliveryId > 0);
                    Assert.Equal(1, await DeliveryIdCountAsync(connection, fixture.Schema, deliveryId, cancellationToken));
                    break;
                }
            case "PublishMessageV2":
                {
                    string queueName = fixture.Name("publish-destination");
                    await CreateTopicAsync(connection, fixture.Schema, entityName, cancellationToken);
                    await CreateQueueAsync(connection, fixture.Schema, queueName, cancellationToken);
                    await CreateQueueSubscriptionAsync(connection, fixture.Schema, entityName, queueName, cancellationToken);
                    long deliveryCount = await PublishAsync(connection, fixture.Schema, entityName, Guid.NewGuid(), cancellationToken);
                    Assert.Equal(1, deliveryCount);
                    Assert.Equal(1, await connection.DeliveryCountAsync(fixture.Schema, queueName, 1, cancellationToken));
                    break;
                }
            case "DeleteMessage":
                {
                    await CreateQueueAsync(connection, fixture.Schema, entityName, cancellationToken);
                    await SendAsync(connection, fixture.Schema, entityName, Guid.NewGuid(), cancellationToken);
                    FetchedDelivery delivery = Assert.Single(await FetchAsync(connection, fixture.Schema, entityName, cancellationToken));
                    Assert.Null(await DeleteAsync(connection, fixture.Schema, delivery.DeliveryId, Guid.NewGuid(), cancellationToken));
                    Assert.Equal(delivery.DeliveryId, await DeleteAsync(connection, fixture.Schema, delivery.DeliveryId, delivery.LockId, cancellationToken));
                    Assert.Equal(0, await DeliveryIdCountAsync(connection, fixture.Schema, delivery.DeliveryId, cancellationToken));
                    break;
                }
            case "TouchQueue":
                {
                    long queueId = await CreateQueueAsync(connection, fixture.Schema, entityName, cancellationToken);
                    Assert.Equal(queueId, await TouchQueueAsync(connection, fixture.Schema, entityName, cancellationToken));
                    Assert.Equal((1, 0L), await DeadLetterMetricAsync(connection, fixture.Schema, queueId, cancellationToken));
                    break;
                }
            case "DeadLetterMessages":
                {
                    await CreateQueueAsync(connection, fixture.Schema, entityName, cancellationToken);
                    long deliveryId = await SendAsync(connection, fixture.Schema, entityName, Guid.NewGuid(), cancellationToken);
                    await ExhaustAsync(connection, fixture.Schema, deliveryId, cancellationToken);
                    Assert.Equal(1, await DeadLetterAsync(connection, fixture.Schema, entityName, 10, cancellationToken));
                    break;
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(procedure), procedure, null);
        }
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0122", "sqlserver-native-owner")]
    public async Task ConcurrentQueueDeclarationsCreateOneRowForEachQueueTypeAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using SqlServerTestDatabase fixture = await SqlServerTestDatabase.CreateAsync(
            "concurrent-queue",
            cancellationToken);
        string queueName = fixture.Name("shared");
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var allReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int ready = 0;

        async Task<long> CreateConcurrentlyAsync()
        {
            await using SqlConnection connection = fixture.CreateConnection();
            await connection.OpenWithinAsync(fixture.OperationTimeout, cancellationToken);
            if (Interlocked.Increment(ref ready) == 8)
                allReady.TrySetResult();
            await release.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            return await CreateQueueWithTransientRetryAsync(
                connection,
                fixture.Schema,
                queueName,
                cancellationToken);
        }

        Task<long>[] declarations = Enumerable.Range(0, 8).Select(_ => CreateConcurrentlyAsync()).ToArray();
        await allReady.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
        release.SetResult();
        long[] queueIds = await Task.WhenAll(declarations).WaitAsync(fixture.OperationTimeout, cancellationToken);

        Assert.Single(queueIds.Distinct());
        await using SqlConnection inspection = fixture.CreateConnection();
        await inspection.OpenWithinAsync(fixture.OperationTimeout, cancellationToken);
        Assert.Equal(3, await QueueRowCountAsync(inspection, fixture.Schema, queueName, cancellationToken));
        Assert.True(await QueueIndexIsUniqueAsync(inspection, fixture.Schema, cancellationToken));
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0123", "sqlserver-native-owner")]
    public async Task DeletingMiddleTopicRemovesIncomingAndOutgoingSubscriptionsAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using SqlServerTestDatabase fixture = await SqlServerTestDatabase.CreateAsync(
            "topic-cascade",
            cancellationToken);
        await using SqlConnection connection = fixture.CreateConnection();
        await connection.OpenWithinAsync(fixture.OperationTimeout, cancellationToken);
        string source = fixture.Name("source");
        string middle = fixture.Name("middle");
        string destination = fixture.Name("destination");
        long sourceId = await CreateTopicAsync(connection, fixture.Schema, source, cancellationToken);
        long middleId = await CreateTopicAsync(connection, fixture.Schema, middle, cancellationToken);
        long destinationId = await CreateTopicAsync(connection, fixture.Schema, destination, cancellationToken);
        await CreateTopicSubscriptionAsync(connection, fixture.Schema, source, middle, cancellationToken);
        await CreateTopicSubscriptionAsync(connection, fixture.Schema, middle, destination, cancellationToken);
        Assert.Equal(2, await SubscriptionCountAsync(connection, fixture.Schema, middleId, cancellationToken));

        await using (var delete = new SqlCommand($"DELETE FROM [{fixture.Schema}].[Topic] WHERE Id = @id", connection))
        {
            delete.Parameters.AddWithValue("id", middleId);
            Assert.Equal(1, await delete.ExecuteNonQueryAsync(cancellationToken));
        }

        Assert.Equal(0, await SubscriptionCountAsync(connection, fixture.Schema, middleId, cancellationToken));
        Assert.Equal(2, await TopicCountAsync(connection, fixture.Schema, sourceId, destinationId, cancellationToken));
    }

    private static async Task WaitForProcedureExecutionAsync(
        SqlConnection connection,
        SqlServerTestDatabase fixture,
        string procedure,
        CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(fixture.OperationTimeout);
        while (!timeout.IsCancellationRequested)
        {
            await using var command = new SqlCommand(
                "SELECT COALESCE(SUM(execution_count), 0) FROM sys.dm_exec_procedure_stats "
                + "WHERE database_id = DB_ID() AND object_id = OBJECT_ID(@procedure)",
                connection);
            command.Parameters.AddWithValue("procedure", $"[{fixture.Schema}].[{procedure}]");
            if (Convert.ToInt64(await command.ExecuteScalarAsync(timeout.Token)) > 0)
                return;
            await Task.Yield();
        }

        timeout.Token.ThrowIfCancellationRequested();
    }

    private static async Task<int> OrphanCountAsync(
        SqlConnection connection,
        SqlServerTestDatabase fixture,
        Guid id,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(
            $"SELECT COUNT(*) FROM [{fixture.Schema}].[Message] WHERE TransportMessageId = @id",
            connection);
        command.Parameters.AddWithValue("id", id);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static async Task<long> CreateQueueAsync(
        SqlConnection connection,
        string schema,
        string queueName,
        CancellationToken cancellationToken) =>
        await ExecuteScalarAsync(connection, schema, "CreateQueueV2", cancellationToken, ("QueueName", queueName));

    private static async Task<long> CreateQueueWithTransientRetryAsync(
        SqlConnection connection,
        string schema,
        string queueName,
        CancellationToken cancellationToken)
    {
        const int retryLimit = 10;

        for (int attempt = 0; ; attempt++)
        {
            try
            {
                return await CreateQueueAsync(connection, schema, queueName, cancellationToken);
            }
            catch (SqlException exception) when (
                attempt < retryLimit
                && SqlServerDbConnectionContext.IsTransientErrorNumber(exception.Number))
            {
                // The product uses the same immediate, bounded transient retry policy. The test invokes
                // the procedure directly so that it can synchronize all declarations at the database
                // boundary; preserving that policy here keeps a legitimate 1205 victim from turning the
                // uniqueness oracle into a scheduler-dependent result.
            }
        }
    }

    private static async Task<long> CreateTopicAsync(
        SqlConnection connection,
        string schema,
        string topicName,
        CancellationToken cancellationToken) =>
        await ExecuteScalarAsync(connection, schema, "CreateTopic", cancellationToken, ("TopicName", topicName));

    private static async Task<long> CreateQueueSubscriptionAsync(
        SqlConnection connection,
        string schema,
        string topicName,
        string queueName,
        CancellationToken cancellationToken) =>
        await ExecuteScalarAsync(connection, schema, "CreateQueueSubscription", cancellationToken,
            ("SourceTopicName", topicName), ("DestinationQueueName", queueName));

    private static async Task<long> CreateTopicSubscriptionAsync(
        SqlConnection connection,
        string schema,
        string source,
        string destination,
        CancellationToken cancellationToken) =>
        await ExecuteScalarAsync(connection, schema, "CreateTopicSubscription", cancellationToken,
            ("SourceTopicName", source), ("DestinationTopicName", destination));

    private static async Task<long> SendAsync(
        SqlConnection connection,
        string schema,
        string queueName,
        Guid messageId,
        CancellationToken cancellationToken) =>
        await ExecuteScalarAsync(connection, schema, "SendMessageV2", cancellationToken,
            ("entityName", queueName), ("transportMessageId", Guid.NewGuid()), ("messageId", messageId),
            ("sentTime", DateTimeOffset.UtcNow));

    private static async Task<long> PublishAsync(
        SqlConnection connection,
        string schema,
        string topicName,
        Guid messageId,
        CancellationToken cancellationToken) =>
        await ExecuteScalarAsync(connection, schema, "PublishMessageV2", cancellationToken,
            ("entityName", topicName), ("transportMessageId", Guid.NewGuid()), ("messageId", messageId),
            ("sentTime", DateTimeOffset.UtcNow));

    private static async Task ExhaustAsync(
        SqlConnection connection,
        string schema,
        long deliveryId,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(
            $"UPDATE [{schema}].[MessageDelivery] SET DeliveryCount = MaxDeliveryCount WHERE MessageDeliveryId = @id",
            connection);
        command.Parameters.AddWithValue("id", deliveryId);
        Assert.Equal(1, await command.ExecuteNonQueryAsync(cancellationToken));
    }

    private static async Task<long> DeadLetterAsync(
        SqlConnection connection,
        string schema,
        string queueName,
        int batchSize,
        CancellationToken cancellationToken) =>
        await ExecuteScalarAsync(connection, schema, "DeadLetterMessages", cancellationToken,
            ("queueName", queueName), ("messageCount", batchSize));

    private static async Task<long> TouchQueueAsync(
        SqlConnection connection,
        string schema,
        string queueName,
        CancellationToken cancellationToken) =>
        await ExecuteScalarAsync(connection, schema, "TouchQueue", cancellationToken, ("queueName", queueName));

    private static async Task<FetchedDelivery[]> FetchAsync(
        SqlConnection connection,
        string schema,
        string queueName,
        CancellationToken cancellationToken)
    {
        await using SqlCommand command = Procedure(connection, schema, "FetchMessages",
            ("queueName", queueName), ("consumerId", Guid.NewGuid()), ("lockId", Guid.NewGuid()),
            ("lockDuration", 60), ("fetchCount", 10));
        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<FetchedDelivery>();
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new FetchedDelivery(
                reader.GetInt64(reader.GetOrdinal("MessageDeliveryId")),
                reader.GetGuid(reader.GetOrdinal("LockId"))));
        }
        return result.ToArray();
    }

    private static async Task<long?> DeleteAsync(
        SqlConnection connection,
        string schema,
        long deliveryId,
        Guid lockId,
        CancellationToken cancellationToken)
    {
        await using SqlCommand command = Procedure(connection, schema, "DeleteMessage",
            ("messageDeliveryId", deliveryId), ("lockId", lockId));
        object? value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : Convert.ToInt64(value);
    }

    private static async Task<(int Rows, long Count)> DeadLetterMetricAsync(
        SqlConnection connection,
        string schema,
        long queueId,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(
            $"SELECT COUNT(*), COALESCE(SUM(DeadLetterCount), 0) FROM [{schema}].[QueueMetricCapture] WHERE QueueId = @queueId",
            connection);
        command.Parameters.AddWithValue("queueId", queueId);
        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        Assert.True(await reader.ReadAsync(cancellationToken));
        return (reader.GetInt32(0), reader.GetInt64(1));
    }

    private static async Task<int> DeliveryIdCountAsync(
        SqlConnection connection,
        string schema,
        long deliveryId,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(
            $"SELECT COUNT(*) FROM [{schema}].[MessageDelivery] WHERE MessageDeliveryId = @id",
            connection);
        command.Parameters.AddWithValue("id", deliveryId);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static async Task<int> QueueRowCountAsync(
        SqlConnection connection,
        string schema,
        string queueName,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(
            $"SELECT COUNT(*) FROM [{schema}].[Queue] WHERE Name = @name GROUP BY Name HAVING COUNT(DISTINCT Type) = 3",
            connection);
        command.Parameters.AddWithValue("name", queueName);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static async Task<bool> QueueIndexIsUniqueAsync(
        SqlConnection connection,
        string schema,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(
            "SELECT is_unique FROM sys.indexes WHERE object_id = OBJECT_ID(@table) AND name = 'IX_Queue_Name_Type'",
            connection);
        command.Parameters.AddWithValue("table", $"[{schema}].[Queue]");
        return Convert.ToBoolean(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static async Task<int> SubscriptionCountAsync(
        SqlConnection connection,
        string schema,
        long topicId,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(
            $"SELECT COUNT(*) FROM [{schema}].[TopicSubscription] WHERE SourceId = @id OR DestinationId = @id",
            connection);
        command.Parameters.AddWithValue("id", topicId);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static async Task<int> TopicCountAsync(
        SqlConnection connection,
        string schema,
        long first,
        long second,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(
            $"SELECT COUNT(*) FROM [{schema}].[Topic] WHERE Id IN (@first, @second)",
            connection);
        command.Parameters.AddWithValue("first", first);
        command.Parameters.AddWithValue("second", second);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static async Task<long> ExecuteScalarAsync(
        SqlConnection connection,
        string schema,
        string procedure,
        CancellationToken cancellationToken,
        params (string Name, object Value)[] parameters)
    {
        await using SqlCommand command = Procedure(connection, schema, procedure, parameters);
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static SqlCommand Procedure(
        SqlConnection connection,
        string schema,
        string procedure,
        params (string Name, object Value)[] parameters)
    {
        var command = new SqlCommand($"[{schema}].[{procedure}]", connection)
        {
            CommandType = CommandType.StoredProcedure,
        };
        foreach ((string name, object value) in parameters)
            command.Parameters.AddWithValue(name, value);
        return command;
    }

    private sealed record FetchedDelivery(long DeliveryId, Guid LockId);
}
