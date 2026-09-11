using System.Collections.Concurrent;
using Npgsql;
using ViciOne.ServiceBus.SqlTransport;
using ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.PostgreSql;

public sealed class PostgreSqlConcurrencyAndPriorityTests
{
    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0096", "postgresql-native-owner")]
    public async Task PartitionedReceive_ThirtyMessagesPreserveOrderWithinBothKeysAtConcurrencyTenAsync()
    {
        const int messageCount = 30;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync(
            "partitioned-order",
            cancellationToken);
        string queueName = fixture.Name("partitioned-input");
        var received = new ConcurrentQueue<(string Key, int Index)>();
        var completed = NewObservation();
        int remaining = messageCount;
        IBusControl bus = SqlBusFactory.Create(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.PrefetchCount = 10;
                endpoint.ConcurrentMessageLimit = 10;
                endpoint.SetReceiveMode(SqlReceiveMode.PartitionedOrdered);
                endpoint.Handler<PartitionedMessage>(context =>
                {
                    string key = context.Advanced().GetPartitionKey()
                        ?? throw new InvalidOperationException("The PostgreSQL delivery lost its partition key.");
                    received.Enqueue((key, context.Message.Index));
                    if (Interlocked.Decrement(ref remaining) == 0)
                        completed.TrySetResult();
                    return Task.CompletedTask;
                });
            });
        });
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            ISendEndpoint endpoint = await bus.GetSendEndpointAsync(new Uri($"queue:{queueName}"), TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            for (int index = 1; index <= messageCount; index++)
            {
                string key = (index % 2).ToString();
                await endpoint.SendAsync(
                        new PartitionedMessage(index),
                        context => context.SetPartitionKey(key),
                        cancellationToken)
                    .WaitAsync(fixture.OperationTimeout, cancellationToken);
            }

            await completed.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }

        Assert.Equal(messageCount, received.Count);
        Assert.Equal(Enumerable.Range(1, 15).Select(value => value * 2),
            received.Where(item => item.Key == "0").Select(item => item.Index));
        Assert.Equal(Enumerable.Range(0, 15).Select(value => value * 2 + 1),
            received.Where(item => item.Key == "1").Select(item => item.Index));
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0098", "postgresql-native-owner")]
    public async Task ParallelPublish_OneThousandMessagesFromTenPublishersArriveExactlyOnceAsync()
    {
        const int messageCount = 1000;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync(
            "parallel-publish",
            cancellationToken);
        string queueName = fixture.Name("publish-input");
        Guid[] expected = Enumerable.Range(0, messageCount).Select(_ => Guid.NewGuid()).ToArray();
        var counts = new ConcurrentDictionary<Guid, int>();
        var completed = NewObservation();
        int unique = 0;
        IBusControl bus = SqlBusFactory.Create(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.PrefetchCount = 30;
                endpoint.Handler<ParallelPublishMessage>(context =>
                {
                    int count = counts.AddOrUpdate(context.Message.Id, 1, static (_, value) => value + 1);
                    if (count == 1 && Interlocked.Increment(ref unique) == messageCount)
                        completed.TrySetResult();
                    return Task.CompletedTask;
                });
            });
        });
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            await Parallel.ForEachAsync(
                expected,
                new ParallelOptions
                {
                    CancellationToken = cancellationToken,
                    MaxDegreeOfParallelism = 10,
                },
                async (id, token) =>
                    await bus.PublishAsync(new ParallelPublishMessage(id), token)
                        .WaitAsync(fixture.OperationTimeout, token));
            await completed.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }

        Assert.Equal(expected.Order(), counts.Keys.Order());
        Assert.All(counts, entry => Assert.Equal(1, entry.Value));
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0132", "postgresql-native-owner")]
    public async Task PrioritySend_PersistsBelowDefaultAndAboveValuesAndDeliversInAscendingOrderAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync(
            "priority-order",
            cancellationToken);
        string queueName = fixture.Name("priority-input");
        await CreateQueueAsync(fixture, queueName, cancellationToken);

        var messages = new[]
        {
            new PriorityMessage(Guid.NewGuid(), "above-default", 150),
            new PriorityMessage(Guid.NewGuid(), "default", 100),
            new PriorityMessage(Guid.NewGuid(), "below-default", 50),
        };
        IBusControl sender = SqlBusFactory.Create(fixture.ConfigureHost);
        bool senderStarted = false;
        try
        {
            await sender.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            senderStarted = true;
            ISendEndpoint endpoint = await sender.GetSendEndpointAsync(new Uri($"queue:{queueName}"), TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            foreach (PriorityMessage message in messages)
            {
                await endpoint.SendAsync(
                        message,
                        context =>
                        {
                            context.MessageId = message.Id;
                            context.SetPriority(message.Priority);
                        },
                        cancellationToken)
                    .WaitAsync(fixture.OperationTimeout, cancellationToken);
            }
        }
        finally
        {
            if (senderStarted)
                await sender.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }

        await using (NpgsqlConnection connection = fixture.CreateConnection())
        {
            await connection.OpenWithinAsync(fixture.OperationTimeout, cancellationToken);
            IReadOnlyDictionary<Guid, short> stored = await StoredPrioritiesAsync(
                connection,
                fixture.Schema,
                messages.Select(message => message.Id).ToArray(),
                cancellationToken);
            Assert.Equal(50, stored[messages[2].Id]);
            Assert.Equal(100, stored[messages[1].Id]);
            Assert.Equal(150, stored[messages[0].Id]);
        }

        var delivered = new ConcurrentQueue<(string Value, short Priority, bool HasPriorityProperty)>();
        var completed = NewObservation();
        int remaining = messages.Length;
        IBusControl receiver = SqlBusFactory.Create(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.ConfigureConsumeTopology = false;
                endpoint.PrefetchCount = 1;
                endpoint.ConcurrentMessageLimit = 1;
                endpoint.Handler<PriorityMessage>(context =>
                {
                    SqlReceiveContext receiveContext = context.GetPayload<SqlReceiveContext>();
                    bool hasPriorityProperty = receiveContext.GetTransportProperties()?.ContainsKey("SQL-Priority") == true;
                    delivered.Enqueue((context.Message.Value, receiveContext.Priority, hasPriorityProperty));
                    if (Interlocked.Decrement(ref remaining) == 0)
                        completed.TrySetResult();
                    return Task.CompletedTask;
                });
            });
        });
        bool receiverStarted = false;
        try
        {
            await receiver.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            receiverStarted = true;
            await completed.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
        finally
        {
            if (receiverStarted)
                await receiver.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }

        Assert.Equal(
            [
                ("below-default", (short)50, true),
                ("default", (short)100, false),
                ("above-default", (short)150, true),
            ],
            delivered);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0134", "postgresql-native-owner")]
    public async Task TwoReceivers_ConsumeOneSharedQueueWithoutLossOrDuplicatesAsync()
    {
        const int messageCount = 100;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync(
            "two-receivers",
            cancellationToken);
        string queueName = fixture.Name("shared-input");
        Guid[] expected = Enumerable.Range(0, messageCount).Select(_ => Guid.NewGuid()).ToArray();
        var counts = new ConcurrentDictionary<Guid, int>();
        var ownerCounts = new ConcurrentDictionary<string, int>();
        var firstReceiverEntered = NewObservation();
        var secondReceiverEntered = NewObservation();
        var releaseFirstReceiver = NewObservation();
        var completed = NewObservation();
        int unique = 0;

        IBusControl first = CreateCompetingReceiver(
            fixture,
            queueName,
            async context =>
            {
                firstReceiverEntered.TrySetResult();
                await releaseFirstReceiver.Task.WaitAsync(fixture.OperationTimeout, context.CancellationToken);
                Record(context.Message.Id, "first");
            });
        IBusControl second = CreateCompetingReceiver(
            fixture,
            queueName,
            context =>
            {
                secondReceiverEntered.TrySetResult();
                Record(context.Message.Id, "second");
                return Task.CompletedTask;
            });
        bool firstStarted = false;
        bool secondStarted = false;

        try
        {
            await first.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            firstStarted = true;
            ISendEndpoint endpoint = await first.GetSendEndpointAsync(new Uri($"queue:{queueName}"), TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            await endpoint.SendAsync(new CompetingMessage(expected[0]), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await firstReceiverEntered.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);

            await second.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            secondStarted = true;
            for (int index = 1; index < expected.Length; index++)
            {
                await endpoint.SendAsync(new CompetingMessage(expected[index]), cancellationToken)
                    .WaitAsync(fixture.OperationTimeout, cancellationToken);
            }
            await secondReceiverEntered.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            releaseFirstReceiver.TrySetResult();
            await completed.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
        finally
        {
            releaseFirstReceiver.TrySetResult();
            if (secondStarted)
                await second.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            if (firstStarted)
                await first.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }

        Assert.Equal(expected.Order(), counts.Keys.Order());
        Assert.All(counts, entry => Assert.Equal(1, entry.Value));
        Assert.True(ownerCounts["first"] > 0);
        Assert.True(ownerCounts["second"] > 0);

        void Record(Guid id, string owner)
        {
            int count = counts.AddOrUpdate(id, 1, static (_, value) => value + 1);
            ownerCounts.AddOrUpdate(owner, 1, static (_, value) => value + 1);
            if (count == 1 && Interlocked.Increment(ref unique) == messageCount)
                completed.TrySetResult();
        }
    }

    private static IBusControl CreateCompetingReceiver(
        PostgreSqlTestDatabase fixture,
        string queueName,
        MessageHandler<CompetingMessage> handler) =>
        SqlBusFactory.Create(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.ConfigureConsumeTopology = false;
                endpoint.PrefetchCount = 1;
                endpoint.ConcurrentMessageLimit = 1;
                endpoint.Handler<CompetingMessage>(handler);
            });
        });

    private static async Task CreateQueueAsync(
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
                endpoint.Handler<PriorityMessage>(_ => Task.CompletedTask);
            });
        });
        await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
        await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
    }

    private static async Task<IReadOnlyDictionary<Guid, short>> StoredPrioritiesAsync(
        NpgsqlConnection connection,
        string schema,
        Guid[] messageIds,
        CancellationToken cancellationToken)
    {
        string commandText = $"SELECT m.message_id, d.priority FROM \"{schema}\".message_delivery d "
            + $"JOIN \"{schema}\".message m ON m.transport_message_id = d.transport_message_id "
            + "WHERE m.message_id = ANY(@messageIds)";
        await using var command = new NpgsqlCommand(commandText, connection);
        command.Parameters.AddWithValue("messageIds", messageIds);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        var values = new Dictionary<Guid, short>();
        while (await reader.ReadAsync(cancellationToken))
            values.Add(reader.GetGuid(0), reader.GetInt16(1));
        return values;
    }

    private static TaskCompletionSource NewObservation() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed record PartitionedMessage(int Index);
    private sealed record ParallelPublishMessage(Guid Id);
    private sealed record PriorityMessage(Guid Id, string Value, short Priority);
    private sealed record CompetingMessage(Guid Id);
}
