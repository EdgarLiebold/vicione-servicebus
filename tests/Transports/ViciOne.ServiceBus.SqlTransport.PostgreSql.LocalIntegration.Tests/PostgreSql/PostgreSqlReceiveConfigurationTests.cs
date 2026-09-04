using System.Collections.Concurrent;
using Npgsql;
using ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.PostgreSql;

public sealed class PostgreSqlReceiveConfigurationTests
{
    public static TheoryData<string> SchemaNames =>
    [
        "schema_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
        "schema_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
    ];

    public static TheoryData<SqlReceiveMode, int> ReceiveModes => new()
    {
        { SqlReceiveMode.Normal, 3 },
        { SqlReceiveMode.Partitioned, 1 },
        { SqlReceiveMode.PartitionedConcurrent, 3 },
        { SqlReceiveMode.PartitionedOrdered, 1 },
        { SqlReceiveMode.PartitionedOrderedConcurrent, 3 },
    };

    [Theory]
    [MemberData(nameof(SchemaNames))]
    [RequirementCoverage("OBL-R0-SQL-0106", "postgresql-native-owner")]
    public async Task SchemaNamesOnBothSidesOfTheNotificationBoundary_DeliverExactlyOnce(string schema)
    {
        Assert.True(schema.Length is 39 or > 40);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync(
            "schema-boundary",
            cancellationToken,
            schema);
        string queueName = fixture.Name("schema-input");
        var delivered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var counts = new ConcurrentDictionary<Guid, int>();
        IBusControl bus = SqlBusFactory.Create(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
                endpoint.Handler<SchemaMessage>(context =>
                {
                    counts.AddOrUpdate(context.Message.Id, 1, static (_, count) => count + 1);
                    delivered.TrySetResult();
                    return Task.CompletedTask;
                }));
        });
        bool started = false;
        var message = new SchemaMessage(Guid.NewGuid());
        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            ISendEndpoint endpoint = await bus.GetSendEndpoint(new Uri($"queue:{queueName}"))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await endpoint.Send(message, context => context.MessageId = message.Id, cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await delivered.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }

        Assert.Equal(new Dictionary<Guid, int> { [message.Id] = 1 }, counts);
    }

    [Theory]
    [MemberData(nameof(ReceiveModes))]
    [RequirementCoverage("OBL-R0-SQL-0107", "postgresql-native-owner")]
    public async Task EveryReceiveMode_DeliversTheExactSetAndItsPartitionConcurrency(
        SqlReceiveMode mode,
        int expectedConcurrent)
    {
        const int messageCount = 3;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase fixture = await PostgreSqlTestDatabase.CreateAsync(
            $"mode-{mode}",
            cancellationToken);
        string queueName = fixture.Name("mode-input");
        await CreateQueue(fixture, queueName, cancellationToken);
        await SendModeMessages(fixture, queueName, cancellationToken);

        var enteredExpectedConcurrency = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = new ConcurrentQueue<int>();
        int active = 0;
        int maximumActive = 0;
        int completedCount = 0;
        IBusControl receiver = SqlBusFactory.Create(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.PrefetchCount = messageCount;
                endpoint.ConcurrentMessageLimit = messageCount;
                endpoint.SetReceiveMode(mode, messageCount);
                endpoint.Handler<ModeMessage>(async context =>
                {
                    started.Enqueue(context.Message.Index);
                    int current = Interlocked.Increment(ref active);
                    UpdateMaximum(ref maximumActive, current);
                    if (current == expectedConcurrent)
                        enteredExpectedConcurrency.TrySetResult();

                    await release.Task.WaitAsync(context.CancellationToken);

                    Interlocked.Decrement(ref active);
                    if (Interlocked.Increment(ref completedCount) == messageCount)
                        completed.TrySetResult();
                });
            });
        });
        bool receiverStarted = false;
        try
        {
            await receiver.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            receiverStarted = true;
            await enteredExpectedConcurrency.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            release.TrySetResult();
            await completed.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
        finally
        {
            release.TrySetResult();
            if (receiverStarted)
                await receiver.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }

        Assert.Equal([1, 2, 3], started.Order());
        if (expectedConcurrent == 1)
            Assert.Equal([1, 2, 3], started);
        Assert.Equal(expectedConcurrent, maximumActive);
        await using NpgsqlConnection connection = fixture.CreateConnection();
        await connection.OpenWithin(fixture.OperationTimeout, cancellationToken);
        Assert.Equal(0, await connection.DeliveryCount(fixture.Schema, queueName, 1, cancellationToken));
    }

    private static async Task CreateQueue(
        PostgreSqlTestDatabase fixture,
        string queueName,
        CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = fixture.CreateConnection();
        await connection.OpenWithin(fixture.OperationTimeout, cancellationToken);
        await using var command = new NpgsqlCommand(
            $"SELECT \"{fixture.Schema}\".create_queue_v2(@queueName, NULL, NULL)",
            connection);
        command.Parameters.AddWithValue("queueName", queueName);
        Assert.True(Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken)) > 0);
    }

    private static async Task SendModeMessages(
        PostgreSqlTestDatabase fixture,
        string queueName,
        CancellationToken cancellationToken)
    {
        IBusControl sender = SqlBusFactory.Create(fixture.ConfigureHost);
        bool started = false;
        try
        {
            await sender.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            ISendEndpoint endpoint = await sender.GetSendEndpoint(new Uri($"queue:{queueName}"))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            for (int index = 1; index <= 3; index++)
            {
                var message = new ModeMessage(Guid.NewGuid(), index);
                await endpoint.Send(
                        message,
                        context =>
                        {
                            context.MessageId = message.Id;
                            context.SetPartitionKey("same-partition");
                        },
                        cancellationToken)
                    .WaitAsync(fixture.OperationTimeout, cancellationToken);
            }
        }
        finally
        {
            if (started)
                await sender.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    private static void UpdateMaximum(ref int maximum, int value)
    {
        int observed;
        do
            observed = Volatile.Read(ref maximum);
        while (value > observed && Interlocked.CompareExchange(ref maximum, value, observed) != observed);
    }

    private sealed record SchemaMessage(Guid Id);
    private sealed record ModeMessage(Guid Id, int Index);
}
