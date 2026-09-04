using System.Collections.Concurrent;
using ViciOne.ServiceBus.SqlTransport;
using ViciOne.ServiceBus.SqlTransport.SqlServer.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.SqlServer.LocalIntegration.Tests.SqlServer;

public sealed class SqlServerConcurrencyTests
{
    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0097", "sqlserver-native-owner")]
    public async Task PartitionedReceive_ThirtyMessagesPreserveOrderWithinBothKeysAtConcurrencyTen()
    {
        const int messageCount = 30;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using SqlServerTestDatabase fixture = await SqlServerTestDatabase.CreateAsync(
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
                    string key = context.PartitionKey()
                        ?? throw new InvalidOperationException("The SQL Server delivery lost its partition key.");
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
            ISendEndpoint endpoint = await bus.GetSendEndpoint(new Uri($"queue:{queueName}"))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            for (int index = 1; index <= messageCount; index++)
            {
                string key = (index % 2).ToString();
                await endpoint.Send(
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
    [RequirementCoverage("OBL-R0-SQL-0099", "sqlserver-native-owner")]
    public async Task ParallelPublish_OneThousandMessagesFromTenPublishersArriveExactlyOnce()
    {
        const int messageCount = 1000;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using SqlServerTestDatabase fixture = await SqlServerTestDatabase.CreateAsync(
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
                    await bus.Publish(new ParallelPublishMessage(id), token)
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

    private static TaskCompletionSource NewObservation() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed record PartitionedMessage(int Index);
    private sealed record ParallelPublishMessage(Guid Id);
}
