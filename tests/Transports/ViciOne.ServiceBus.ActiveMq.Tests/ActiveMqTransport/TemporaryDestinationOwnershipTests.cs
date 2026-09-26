using Apache.NMS;
using ViciOne.ServiceBus.ActiveMq.Configuration;
using ViciOne.ServiceBus.ActiveMq.Tests.TestDoubles;
using ViciOne.ServiceBus.ActiveMq.Topology;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMq.Tests.ActiveMqTransport;

public sealed class TemporaryDestinationOwnershipTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-LIFECYCLE", "concurrent-temporary-resolution-creates-one-native-entity")]
    public async Task ConcurrentCreators_ShareOneOwnedDestinationAsync(bool topic)
    {
        const int callerCount = 8;
        using var ready = new CountdownEvent(callerCount);
        using var start = new ManualResetEventSlim();
        using var releaseCreation = new ManualResetEventSlim();
        var creationEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int created = 0;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        IConnection nativeConnection = InterfaceProxy<IConnection>.Create((method, _) => method.Name switch
        {
            nameof(IConnection.CloseAsync) => Task.CompletedTask,
            nameof(IDisposable.Dispose) => null,
            _ => throw new InvalidOperationException($"Unexpected connection operation: {method.Name}"),
        });
        var topology = new ActiveMqTopologyConfiguration(ActiveMqBusFactory.CreateMessageTopology());
        var configuration = new ActiveMqBusConfiguration(topology);
        configuration.HostConfiguration.Settings = new OpenWireHostSettings(new Uri("activemq://broker.internal:61616"));
        await using var connection = new ActiveMqConnectionContext(nativeConnection, configuration.HostConfiguration, CancellationToken.None);
        Task<IDestination>[] callers = Enumerable.Range(0, callerCount).Select(_ => Task.Factory.StartNew(() =>
        {
            ISession session = InterfaceProxy<ISession>.Create((method, _) => method.Name switch
            {
                nameof(ISession.CreateTemporaryQueue) when !topic => CreateNative(),
                nameof(ISession.CreateTemporaryTopic) when topic => CreateNative(),
                _ => throw new InvalidOperationException($"Unexpected session operation: {method.Name}"),
            });
            ready.Signal();
            start.Wait(cancellationToken);
            return topic
                ? (IDestination)connection.GetTemporaryTopic(session, "shared")
                : connection.GetTemporaryQueue(session, "shared");
        }, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray();

        try
        {
            Assert.True(ready.Wait(TimeSpan.FromSeconds(10), cancellationToken));
            start.Set();
            await creationEntered.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
        }
        finally
        {
            start.Set();
            releaseCreation.Set();
            await Task.WhenAll(callers).WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
        }

        IDestination[] destinations = await Task.WhenAll(callers);
        Assert.Equal(1, Volatile.Read(ref created));
        Assert.All(destinations, destination => Assert.Same(destinations[0], destination));
        Assert.True(connection.TryGetTemporaryEntity("shared", out IDestination? registered));
        Assert.Same(destinations[0], registered);

        IDestination CreateNative()
        {
            int identity = Interlocked.Increment(ref created);
            creationEntered.TrySetResult();
            releaseCreation.Wait(cancellationToken);
            return topic ? TemporaryTopic(identity) : TemporaryQueue(identity);
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-LIFECYCLE", "temporary-send-and-consume-share-one-native-destination")]
    public async Task SendAndConsume_ResolveTheSameTemporaryDestinationRegardlessOfStartupOrderAsync(bool topic, bool sendFirst)
    {
        const string logicalName = "VirtualTopic.reply-cache";
        int created = 0;
        IConnection nativeConnection = InterfaceProxy<IConnection>.Create((method, _) => method.Name switch
        {
            nameof(IConnection.CloseAsync) => Task.CompletedTask,
            nameof(IDisposable.Dispose) => null,
            _ => throw new InvalidOperationException($"Unexpected connection operation: {method.Name}"),
        });
        var topology = new ActiveMqTopologyConfiguration(ActiveMqBusFactory.CreateMessageTopology());
        var configuration = new ActiveMqBusConfiguration(topology);
        configuration.HostConfiguration.Settings = new OpenWireHostSettings(new Uri("activemq://broker.internal:61616"));
        await using var connection = new ActiveMqConnectionContext(nativeConnection, configuration.HostConfiguration, CancellationToken.None);
        await using var sender = new ActiveMqSessionContext(connection, NativeSession(), CancellationToken.None);
        await using var receiver = new ActiveMqSessionContext(connection, NativeSession(), CancellationToken.None);
        DestinationType kind = topic ? DestinationType.TemporaryTopic : DestinationType.TemporaryQueue;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        IDestination sendDestination;
        IDestination consumeDestination;
        if (sendFirst)
        {
            sendDestination = await sender.GetDestinationAsync(logicalName, kind, cancellationToken);
            consumeDestination = await ResolveConsumerAsync();
        }
        else
        {
            consumeDestination = await ResolveConsumerAsync();
            sendDestination = await sender.GetDestinationAsync(logicalName, kind, cancellationToken);
        }

        Assert.Same(consumeDestination, sendDestination);
        Assert.Same(sendDestination, sender.GetTemporaryDestination(logicalName));
        Assert.Same(sendDestination, receiver.GetTemporaryDestination(logicalName));
        Assert.Same(sendDestination, await sender.GetDestinationAsync(logicalName, kind, cancellationToken));
        Assert.Equal(1, Volatile.Read(ref created));

        async Task<IDestination> ResolveConsumerAsync()
        {
            if (topic)
            {
                Topic target = InterfaceProxy<Topic>.Create((method, _) => method.Name switch
                {
                    "get_EntityName" => logicalName,
                    "get_Durable" => false,
                    "get_AutoDelete" => true,
                    _ => throw new InvalidOperationException($"Unexpected topic operation: {method.Name}"),
                });
                return await receiver.GetTopicAsync(target, cancellationToken);
            }

            ViciOne.ServiceBus.ActiveMq.Topology.Queue queue = InterfaceProxy<ViciOne.ServiceBus.ActiveMq.Topology.Queue>.Create((method, _) => method.Name switch
            {
                "get_EntityName" => logicalName,
                "get_Durable" => false,
                "get_AutoDelete" => true,
                _ => throw new InvalidOperationException($"Unexpected queue operation: {method.Name}"),
            });
            return await receiver.GetQueueAsync(queue, cancellationToken);
        }

        ISession NativeSession() => InterfaceProxy<ISession>.Create((method, _) => method.Name switch
        {
            nameof(ISession.CreateTemporaryQueue) when !topic => TemporaryQueue(Interlocked.Increment(ref created)),
            nameof(ISession.CreateTemporaryTopic) when topic => TemporaryTopic(Interlocked.Increment(ref created)),
            nameof(ISession.CloseAsync) => Task.CompletedTask,
            nameof(IDisposable.Dispose) => null,
            _ => throw new InvalidOperationException($"Unexpected session operation: {method.Name}"),
        });
    }

    private static ITemporaryQueue TemporaryQueue(int identity) => InterfaceProxy<ITemporaryQueue>.Create((method, _) => method.Name switch
    {
        "get_IsQueue" => true,
        "get_IsTopic" => false,
        "get_IsTemporary" => true,
        "get_QueueName" => $"provider-queue-{identity}",
        _ => throw new InvalidOperationException($"Unexpected queue destination operation: {method.Name}"),
    });

    private static ITemporaryTopic TemporaryTopic(int identity) => InterfaceProxy<ITemporaryTopic>.Create((method, _) => method.Name switch
    {
        "get_IsQueue" => false,
        "get_IsTopic" => true,
        "get_IsTemporary" => true,
        "get_TopicName" => $"provider-topic-{identity}",
        _ => throw new InvalidOperationException($"Unexpected topic destination operation: {method.Name}"),
    });
}
