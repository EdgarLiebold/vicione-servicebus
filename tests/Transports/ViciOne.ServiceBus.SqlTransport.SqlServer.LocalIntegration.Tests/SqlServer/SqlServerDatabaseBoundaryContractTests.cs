using System.Data;
using System.Net.Mime;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Providers.Transports;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.SqlTransport.Configuration;
using ViciOne.ServiceBus.SqlTransport.SqlServer.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.SqlTransport.Topology;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.SqlServer.LocalIntegration.Tests.SqlServer;

public sealed class SqlServerDatabaseBoundaryContractTests
{
    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 0)]
    [InlineData(true, 1)]
    [InlineData(true, 2)]
    [RequirementCoverage("REQ-VSB-SQLSERVER-UNICODE", "legacy-collation-fetch-and-routing-preserve-unicode")]
    public async Task UnicodeMetadata_SurvivesNormalPartitionedFetchAndRoutingAsync(bool unicode, int route)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        await using SqlServerTestDatabase fixture = await SqlServerTestDatabase.CreateAsync("unicode-boundary", token);
        await using SqlConnection connection = fixture.CreateConnection();
        await connection.OpenAsync(token);
        string collation = await ScalarAsync<string>(connection,
            "SELECT CONVERT(nvarchar(128), DATABASEPROPERTYEX(DB_NAME(), 'Collation'))", null, token);
        Assert.DoesNotContain("UTF8", collation, StringComparison.OrdinalIgnoreCase);
        string metadata = unicode ? "Žlutý-東京-🚀" : "ascii-route";
        string queue = fixture.Name("queue");
        string topic = fixture.Name("topic");
        await WithClientAsync(fixture, async client =>
        {
            await client.CreateQueueAsync(new QueueDefinition(queue), token);
            var context = Message(token);
            context.RoutingKey = metadata;
            context.PartitionKey = metadata;
            context.SupportedMessageTypes = [$"urn:message:Boundary:{metadata}"];
            if (route == 2)
            {
                await client.CreateTopicAsync(new TopicDefinition(topic), token);
                await client.CreateQueueSubscriptionAsync(new QueueSubscriptionDefinition(
                    new TopicDefinition(topic), new QueueDefinition(queue), SqlSubscriptionType.RoutingKey, metadata), token);
                Assert.Equal(metadata, await ScalarAsync<string>(connection,
                    $"SELECT RoutingKey FROM [{fixture.Schema}].QueueSubscription", null, token));
                await client.PublishAsync(topic, context, token);
            }
            else
                await client.SendAsync(queue, context, token);

            SqlTransportMessage received = Assert.Single(await client.ReceiveMessagesAsync(queue,
                route == 1 ? SqlReceiveMode.Partitioned : SqlReceiveMode.Normal, 1, 1, TimeSpan.FromSeconds(30), token));
            Assert.Equal(context.TransportMessageId, received.TransportMessageId);
            Assert.Equal(metadata, received.RoutingKey);
            Assert.Equal(metadata, received.PartitionKey);
            Assert.Equal(string.Join(';', context.SupportedMessageTypes), received.MessageType);
            Assert.Equal(new byte[] { 7, 19, 233 }, received.BinaryBody);
            Assert.True(await client.DeleteMessageAsync(received.LockId!.Value, received.MessageDeliveryId, token));
        }, token);
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 1000)]
    [InlineData(false, 1900)]
    [InlineData(true, 0)]
    [InlineData(true, 1000)]
    [InlineData(true, 1900)]
    [RequirementCoverage("REQ-VSB-SQLSERVER-FRACTIONAL-DELAY", "send-and-unlock-do-not-deliver-before-requested-delay")]
    public async Task Delay_PersistsAtLeastTheRequestedIntervalForSendAndUnlockAsync(bool unlock, int milliseconds)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        await using SqlServerTestDatabase fixture = await SqlServerTestDatabase.CreateAsync("fractional-delay", token);
        await using SqlConnection connection = fixture.CreateConnection();
        await connection.OpenAsync(token);
        string queue = fixture.Name("queue");
        await WithClientAsync(fixture, async client =>
        {
            await client.CreateQueueAsync(new QueueDefinition(queue), token);
            var context = Message(token);
            long deliveryId;
            SqlTransportMessage? received = null;
            if (unlock)
            {
                await client.SendAsync(queue, context, token);
                received = Assert.Single(await client.ReceiveMessagesAsync(queue, SqlReceiveMode.Normal, 1, 1,
                    TimeSpan.FromSeconds(30), token));
                deliveryId = received.MessageDeliveryId;
            }
            else
                deliveryId = 0;

            DateTime before = await ScalarAsync<DateTime>(connection, "SELECT SYSUTCDATETIME()", null, token);
            TimeSpan requested = TimeSpan.FromMilliseconds(milliseconds);
            if (unlock)
                Assert.True(await client.UnlockAsync(received!.LockId!.Value, deliveryId, requested,
                    new DictionarySendHeaders(), token));
            else
            {
                context.Delay = requested;
                await client.SendAsync(queue, context, token);
            }
            DateTime after = await ScalarAsync<DateTime>(connection, "SELECT SYSUTCDATETIME()", null, token);
            // Environment precondition, never a defect oracle: a stalled server can hide rounding.
            Assert.True(after - before < TimeSpan.FromMilliseconds(500), "Server observation window must be below 500 ms.");
            DateTime enqueue = await ScalarAsync<DateTime>(connection,
                $"SELECT EnqueueTime FROM [{fixture.Schema}].MessageDelivery WHERE TransportMessageId=@Id",
                new { Id = context.TransportMessageId }, token);
            Assert.InRange(enqueue, before + requested, after + requested + TimeSpan.FromSeconds(1));
            if (unlock)
            {
                Assert.Equal(0, await ScalarAsync<int>(connection,
                    $"SELECT COUNT(*) FROM [{fixture.Schema}].MessageDelivery WHERE TransportMessageId=@Id AND (LockId IS NOT NULL OR ConsumerId IS NOT NULL)",
                    new { Id = context.TransportMessageId }, token));
            }
        }, token);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQLSERVER-SCHEMA-INDEXES", "every-schema-owns-subscription-and-fetch-indexes")]
    public async Task SecondSchema_OwnsItsCompleteIndexSetAndReapplicationIsIdempotentAsync()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        await using SqlServerTestDatabase fixture = await SqlServerTestDatabase.CreateAsync("schema-indexes", token);
        await using SqlConnection connection = fixture.CreateConnection();
        await connection.OpenAsync(token);
        string[] original = await IndexesAsync(connection, fixture.Schema, token);
        var options = new SqlTransportOptions
        {
            Host = fixture.Options.Host, Port = fixture.Options.Port, Database = fixture.Database,
            Schema = "transport_second", Role = fixture.Options.Role, Username = fixture.Options.Username,
            Password = fixture.Options.Password, AdminUsername = fixture.Options.AdminUsername,
            AdminPassword = fixture.Options.AdminPassword, ConnectionString = fixture.Options.ConnectionString,
        };
        var migrator = new SqlServerDatabaseMigrator(NullLogger<SqlServerDatabaseMigrator>.Instance);
        await migrator.CreateSchemaIfNotExistAsync(options, token);
        await migrator.CreateInfrastructureAsync(options, token);
        string[] second = await IndexesAsync(connection, options.Schema!, token);
        // These two are already object-scoped: positive controls, not the alleged defect.
        Assert.Contains("Queue:IX_Queue_Name_Type", second);
        Assert.Contains("Topic:IX_Topic_Name", second);
        string[] required =
        [
            "Queue:IX_Queue_AutoDelete", "TopicSubscription:IX_TopicSubscription_Unique",
            "TopicSubscription:IX_TopicSubscription_Source", "TopicSubscription:IX_TopicSubscription_Destination",
            "QueueSubscription:IX_QueueSubscription_Unique", "QueueSubscription:IX_QueueSubscription_Source",
            "QueueSubscription:IX_QueueSubscription_Destination", "Message:IX_Message_SchedulingTokenId",
            "MessageDelivery:IX_MessageDelivery_Fetch", "MessageDelivery:IX_MessageDelivery_FetchPart",
            "MessageDelivery:IX_MessageDelivery_TransportMessageId", "QueueMetric:IX_QueueMetric_Unique",
        ];
        Assert.All(required, index => Assert.Contains(index, second));
        await migrator.CreateInfrastructureAsync(options, token);
        Assert.Equal(second, await IndexesAsync(connection, options.Schema!, token));
        Assert.Equal(original, await IndexesAsync(connection, fixture.Schema, token));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-SQLSERVER-BIGINT-DELIVERY", "send-and-delete-complete-for-bigint-delivery-identities")]
    public async Task BigintDelivery_SendAndDeleteCompleteWithPersistedIdentitiesAsync(bool high, bool delete)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        await using SqlServerTestDatabase fixture = await SqlServerTestDatabase.CreateAsync("bigint-delivery", token);
        await using SqlConnection connection = fixture.CreateConnection();
        await connection.OpenAsync(token);
        string queue = fixture.Name("queue");
        long expectedId = high ? (long)int.MaxValue + 19 : 19;
        await WithClientAsync(fixture, async client =>
        {
            await client.CreateQueueAsync(new QueueDefinition(queue), token);
            var context = Message(token);
            if (delete)
            {
                // Settle a genuine sent delivery; isolate Delete from a possibly failing high-ID Send.
                await client.SendAsync(queue, context, token);
                await ExecuteAsync(connection,
                    $"UPDATE [{fixture.Schema}].MessageDelivery SET MessageDeliveryId=@Expected WHERE TransportMessageId=@Id",
                    new { Expected = expectedId, Id = context.TransportMessageId }, token);
                SqlTransportMessage received = Assert.Single(await client.ReceiveMessagesAsync(queue,
                    SqlReceiveMode.Normal, 1, 1, TimeSpan.FromSeconds(30), token));
                Assert.Equal(expectedId, received.MessageDeliveryId);
                bool deleted = false;
                Exception? failure = await Record.ExceptionAsync(async () =>
                {
                    deleted = await client.DeleteMessageAsync(received.LockId!.Value, expectedId, token);
                });
                int remaining = await ScalarAsync<int>(connection,
                    $"SELECT COUNT(*) FROM [{fixture.Schema}].MessageDelivery WHERE MessageDeliveryId=@Id",
                    new { Id = expectedId }, token);
                Assert.Null(failure);
                Assert.True(deleted);
                Assert.Equal(0, remaining);
            }
            else
            {
                await ExecuteAsync(connection,
                    $"ALTER SEQUENCE [{fixture.Schema}].DeliverySequence RESTART WITH {expectedId}", null, token);
                Exception? failure = await Record.ExceptionAsync(() => client.SendAsync(queue, context, token));
                long[] persisted = (await connection.QueryAsync<long>(new CommandDefinition(
                    $"SELECT MessageDeliveryId FROM [{fixture.Schema}].MessageDelivery WHERE TransportMessageId=@Id",
                    new { Id = context.TransportMessageId }, cancellationToken: token))).ToArray();
                Assert.Null(failure);
                Assert.Equal(expectedId, Assert.Single(persisted));
            }
        }, token);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [RequirementCoverage("REQ-VSB-SQLSERVER-FRACTIONAL-DELAY", "fractional-fetch-and-renew-locks-never-end-early")]
    public async Task FractionalLocks_KeepAtLeastTheRequestedDatabaseIntervalAsync(int operation)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        await using SqlServerTestDatabase fixture = await SqlServerTestDatabase.CreateAsync("fractional-lock", token);
        await using SqlConnection connection = fixture.CreateConnection();
        await connection.OpenAsync(token);
        string queue = fixture.Name("queue");
        await WithClientAsync(fixture, async client =>
        {
            await client.CreateQueueAsync(new QueueDefinition(queue), token);
            var context = Message(token);
            context.PartitionKey = "lock-boundary";
            await client.SendAsync(queue, context, token);
            TimeSpan requested = TimeSpan.FromMilliseconds(1900);
            SqlTransportMessage? acquired = null;
            if (operation == 2)
                acquired = Assert.Single(await client.ReceiveMessagesAsync(queue, SqlReceiveMode.Normal, 1, 1, TimeSpan.FromSeconds(30), token));
            DateTime before = await ScalarAsync<DateTime>(connection, "SELECT SYSUTCDATETIME()", null, token);
            if (operation == 2)
                Assert.True(await client.RenewLockAsync(acquired!.LockId!.Value, acquired.MessageDeliveryId, requested, token));
            else
                acquired = Assert.Single(await client.ReceiveMessagesAsync(queue,
                    operation == 0 ? SqlReceiveMode.Normal : SqlReceiveMode.PartitionedOrdered, 1, 1, requested, token));
            DateTime after = await ScalarAsync<DateTime>(connection, "SELECT SYSUTCDATETIME()", null, token);
            Assert.True(after - before < TimeSpan.FromMilliseconds(500), "Server observation window must be below 500 ms.");
            Assert.Equal(context.TransportMessageId, acquired!.TransportMessageId);
            Assert.NotNull(acquired.LockId);
            DateTime enqueue = await ScalarAsync<DateTime>(connection,
                $"SELECT EnqueueTime FROM [{fixture.Schema}].MessageDelivery WHERE MessageDeliveryId=@Id",
                new { Id = acquired.MessageDeliveryId }, token);
            Assert.InRange(enqueue, before + requested, after + requested + TimeSpan.FromSeconds(1));
            Assert.True(await client.DeleteMessageAsync(acquired.LockId.Value, acquired.MessageDeliveryId, token));
        }, token);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [RequirementCoverage("REQ-VSB-SQLSERVER-FRACTIONAL-DELAY", "seconds-overflow-rejects-before-persisting-or-updating-delivery")]
    public async Task SecondsOverflow_RejectsBeforePersistingOrUpdatingTheDeliveryAsync(int operation)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        await using SqlServerTestDatabase fixture = await SqlServerTestDatabase.CreateAsync("seconds-overflow", token);
        await using SqlConnection connection = fixture.CreateConnection();
        await connection.OpenAsync(token);
        string queue = fixture.Name("queue");
        await WithClientAsync(fixture, async client =>
        {
            await client.CreateQueueAsync(new QueueDefinition(queue), token);
            var context = Message(token);
            TimeSpan excessive = TimeSpan.FromTicks((long)int.MaxValue * TimeSpan.TicksPerSecond + 1);
            SqlTransportMessage? acquired = null;
            DateTime? originalEnqueue = null;
            if (operation != 0)
            {
                await client.SendAsync(queue, context, token);
                if (operation != 2)
                    acquired = Assert.Single(await client.ReceiveMessagesAsync(queue, SqlReceiveMode.Normal, 1, 1, TimeSpan.FromSeconds(30), token));
                originalEnqueue = await ScalarAsync<DateTime>(connection,
                    $"SELECT EnqueueTime FROM [{fixture.Schema}].MessageDelivery WHERE TransportMessageId=@Id",
                    new { Id = context.TransportMessageId }, token);
            }
            Exception? failure = await Record.ExceptionAsync(async () =>
            {
                if (operation == 0)
                {
                    context.Delay = excessive;
                    await client.SendAsync(queue, context, token);
                }
                else if (operation == 1)
                    await client.UnlockAsync(acquired!.LockId!.Value, acquired.MessageDeliveryId, excessive, new DictionarySendHeaders(), token);
                else if (operation == 2)
                    await client.ReceiveMessagesAsync(queue, SqlReceiveMode.Normal, 1, 1, excessive, token);
                else
                    await client.RenewLockAsync(acquired!.LockId!.Value, acquired.MessageDeliveryId, excessive, token);
            });
            ArgumentOutOfRangeException rejection = Assert.IsType<ArgumentOutOfRangeException>(failure);
            Assert.Equal(operation switch { 0 => "Delay", 1 => "delay", 2 => "lockDuration", _ => "duration" }, rejection.ParamName);
            Assert.Equal(excessive, Assert.IsType<TimeSpan>(rejection.ActualValue));
            if (operation == 0)
            {
                Assert.Equal(0, await ScalarAsync<int>(connection,
                    $"SELECT COUNT(*) FROM [{fixture.Schema}].Message WHERE TransportMessageId=@Id", new { Id = context.TransportMessageId }, token));
                Assert.Equal(0, await ScalarAsync<int>(connection,
                    $"SELECT COUNT(*) FROM [{fixture.Schema}].MessageDelivery WHERE TransportMessageId=@Id", new { Id = context.TransportMessageId }, token));
            }
            else
            {
                Assert.Equal(originalEnqueue, await ScalarAsync<DateTime>(connection,
                    $"SELECT EnqueueTime FROM [{fixture.Schema}].MessageDelivery WHERE TransportMessageId=@Id", new { Id = context.TransportMessageId }, token));
                if (acquired is not null)
                {
                    Assert.Equal(acquired.LockId!.Value, await ScalarAsync<Guid>(connection,
                        $"SELECT LockId FROM [{fixture.Schema}].MessageDelivery WHERE MessageDeliveryId=@Id", new { Id = acquired.MessageDeliveryId }, token));
                    Assert.True(await client.DeleteMessageAsync(acquired.LockId.Value, acquired.MessageDeliveryId, token));
                }
                else
                    Assert.Equal(0, await ScalarAsync<int>(connection,
                        $"SELECT COUNT(*) FROM [{fixture.Schema}].MessageDelivery WHERE TransportMessageId=@Id AND (LockId IS NOT NULL OR ConsumerId IS NOT NULL)",
                        new { Id = context.TransportMessageId }, token));
            }
        }, token);
    }

    private static async Task WithClientAsync(SqlServerTestDatabase fixture, Func<ClientContext, Task> action, CancellationToken token)
    {
        var bus = new SqlBusConfiguration(new SqlTopologyConfiguration(SqlBusFactory.CreateMessageTopology()));
        var host = Assert.IsType<SqlHostConfiguration>(bus.HostConfiguration);
        host.Settings = new SqlServerHostSettings(fixture.Options) { MaintenanceEnabled = false };
        IConnectionContextSupervisor supervisor = host.ConnectionContextSupervisor;
        try
        {
            await supervisor.SendAsync(Pipe.ExecuteAwaited<ConnectionContext>(context =>
                action(context.CreateClientContext(token))), token);
        }
        finally
        {
            await supervisor.StopAsync("SQL boundary test cleanup", CancellationToken.None);
        }
    }

    private static SqlMessageSendContext<BoundaryMessage> Message(CancellationToken token) =>
        new(new BoundaryMessage(Guid.NewGuid()), token) { Serializer = new BinarySerializer(), Priority = 0 };

    private static async Task<T> ScalarAsync<T>(SqlConnection connection, string sql, object? parameters, CancellationToken token) =>
        Assert.IsType<T>(await connection.ExecuteScalarAsync<object>(new CommandDefinition(sql, parameters, cancellationToken: token)));

    private static Task<int> ExecuteAsync(SqlConnection connection, string sql, object? parameters, CancellationToken token) =>
        connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: token));

    private static async Task<string[]> IndexesAsync(SqlConnection connection, string schema, CancellationToken token) =>
        (await connection.QueryAsync<string>(new CommandDefinition("""
            SELECT o.name + ':' + i.name FROM sys.indexes i
            JOIN sys.objects o ON o.object_id=i.object_id JOIN sys.schemas s ON s.schema_id=o.schema_id
            WHERE s.name=@Schema AND i.name IS NOT NULL AND i.is_primary_key=0 ORDER BY o.name,i.name
            """, new { Schema = schema }, cancellationToken: token))).ToArray();

    private sealed record QueueDefinition(string QueueName) : Queue
    {
        public TimeSpan? AutoDeleteOnIdle => null;
        public int? MaxDeliveryCount => 10;
    }
    private sealed record TopicDefinition(string TopicName) : Topic;
    private sealed record QueueSubscriptionDefinition(Topic Source, Queue Destination,
        SqlSubscriptionType SubscriptionType, string RoutingKey) : TopicToQueueSubscription;
    public sealed record BoundaryMessage(Guid Id);
    private sealed class BinarySerializer : IMessageSerializer
    {
        public ContentType ContentType { get; } = new("application/octet-stream");
        public MessageBody GetMessageBody<T>(SendContext<T> context) where T : class => new BinaryMessageBody(new byte[] { 7, 19, 233 });
    }
}
