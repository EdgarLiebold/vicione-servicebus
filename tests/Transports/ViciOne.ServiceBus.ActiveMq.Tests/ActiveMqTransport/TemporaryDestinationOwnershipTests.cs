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
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-LIFECYCLE", "native-request-reply-address-ignores-same-name-topic")]
    public async Task RequestSend_UsesTheReplyQueueDespiteASameNameTopicAsync(bool queueAlreadyCreated)
    {
        const string name = "reply-name";
        var topology = new ActiveMqTopologyConfiguration(ActiveMqBusFactory.CreateMessageTopology());
        var configuration = new ActiveMqBusConfiguration(topology);
        configuration.HostConfiguration.Settings = new OpenWireHostSettings(new Uri("activemq://broker.internal:61616"));
        IConnection nativeConnection = InterfaceProxy<IConnection>.Create((method, _) => method.Name switch
        {
            nameof(IConnection.CloseAsync) => Task.CompletedTask,
            nameof(IDisposable.Dispose) => null,
            _ => throw new InvalidOperationException($"Unexpected connection operation: {method.Name}"),
        });
        await using var connection = new ActiveMqConnectionContext(nativeConnection, configuration.HostConfiguration, CancellationToken.None);
        ITemporaryQueue replyQueue = TemporaryQueue(1);
        ITemporaryTopic conflictingTopic = TemporaryTopic(2);
        var serviceQueue = new Apache.NMS.ActiveMQ.Commands.ActiveMQQueue("service");
        IMessage? sent = null;
        int queueCreations = 0;
        IMessageProducer producer = InterfaceProxy<IMessageProducer>.Create((method, args) => method.Name switch
        {
            nameof(IMessageProducer.SendAsync) => CaptureAsync(Assert.IsAssignableFrom<IMessage>(args![0])),
            nameof(IMessageProducer.CloseAsync) => Task.CompletedTask,
            nameof(IDisposable.Dispose) => null,
            _ => throw new InvalidOperationException($"Unexpected producer operation: {method.Name}"),
        });
        ISession nativeSession = InterfaceProxy<ISession>.Create((method, args) => method.Name switch
        {
            nameof(ISession.CreateTemporaryTopic) => conflictingTopic,
            nameof(ISession.CreateTemporaryQueue) => CreateReplyQueue(),
            nameof(ISession.CreateBytesMessage) => new Apache.NMS.ActiveMQ.Commands.ActiveMQBytesMessage { Content = (byte[])args![0]! },
            nameof(ISession.CreateProducerAsync) when ReferenceEquals(args![0], serviceQueue) => Task.FromResult(producer),
            nameof(ISession.CloseAsync) => Task.CompletedTask,
            nameof(IDisposable.Dispose) => null,
            _ => throw new InvalidOperationException($"Unexpected session operation: {method.Name}"),
        });
        await using var session = new ActiveMqSessionContext(connection, nativeSession, CancellationToken.None);
        await session.GetDestinationAsync(name, DestinationType.TemporaryTopic, TestContext.Current.CancellationToken);
        if (queueAlreadyCreated)
            await session.GetDestinationAsync(name, DestinationType.TemporaryQueue, TestContext.Current.CancellationToken);

        var serialization = InterfaceProxy<ISerialization>.Create((method, _) =>
            throw new InvalidOperationException($"Unexpected serialization operation: {method.Name}"));
        var endpoint = InterfaceProxy<ViciOne.ServiceBus.Transports.ReceiveEndpointContext>.Create((method, _) => method.Name switch
        {
            "get_Serialization" => serialization,
            _ => throw new InvalidOperationException($"Unexpected endpoint operation: {method.Name}"),
        });
        var supervisor = InterfaceProxy<ISessionContextSupervisor>.Create((method, _) =>
            throw new InvalidOperationException($"Unexpected supervisor operation: {method.Name}"));
        var transport = new ActiveMqSendTransportContext(configuration.HostConfiguration, endpoint, supervisor,
            Pipe.Empty<SessionContext>(), "service", DestinationType.Queue);
        var context = new TransportActiveMqSendContext<ReplyRequest>(new ReplyRequest("request-17"), TestContext.Current.CancellationToken)
        {
            ReplyDestination = serviceQueue,
            ResponseAddress = new Uri($"activemq://broker.internal/{name}?temporary=true"),
            Serializer = new ViciOne.ServiceBus.Serialization.SystemTextJsonRawMessageSerializer(System.Text.Json.JsonSerializerOptions.Default),
        };

        await transport.SendAsync(session, context, TestContext.Current.CancellationToken);

        Assert.NotNull(sent);
        ITemporaryQueue nativeReply = Assert.IsAssignableFrom<ITemporaryQueue>(sent.NMSReplyTo);
        Assert.Equal(replyQueue.QueueName, nativeReply.QueueName);
        Assert.Equal(1, queueCreations);
        Assert.Same(replyQueue, session.GetTemporaryDestination(name, DestinationType.TemporaryQueue));
        Assert.Same(conflictingTopic, session.GetTemporaryDestination(name, DestinationType.TemporaryTopic));

        ITemporaryQueue CreateReplyQueue()
        {
            queueCreations++;
            return replyQueue;
        }

        Task CaptureAsync(IMessage message)
        {
            Assert.Null(sent);
            sent = message;
            return Task.CompletedTask;
        }
    }

    public sealed record ReplyRequest(string Value);

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-LIFECYCLE", "same-name-temporary-queue-and-topic-have-independent-ownership")]
    public async Task SameNameQueueAndTopic_KeepIndependentIdentityAndCleanupAsync(bool topicFirst, bool failFirstDelete)
    {
        const string name = "same-name";
        int created = 0;
        var deleted = new List<IDestination>();
        var deleteFailure = new NMSException("native delete failed");
        bool failDelete = failFirstDelete;
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
        ISession nativeSession = InterfaceProxy<ISession>.Create((method, args) => method.Name switch
        {
            nameof(ISession.CreateTemporaryQueue) => TemporaryQueue(++created),
            nameof(ISession.CreateTemporaryTopic) => TemporaryTopic(++created),
            nameof(ISession.DeleteDestination) => Delete(Assert.IsAssignableFrom<IDestination>(args![0])),
            nameof(ISession.CloseAsync) => Task.CompletedTask,
            nameof(IDisposable.Dispose) => null,
            _ => throw new InvalidOperationException($"Unexpected session operation: {method.Name}"),
        });
        await using var session = new ActiveMqSessionContext(connection, nativeSession, CancellationToken.None);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        DestinationType firstKind = topicFirst ? DestinationType.TemporaryTopic : DestinationType.TemporaryQueue;
        DestinationType secondKind = topicFirst ? DestinationType.TemporaryQueue : DestinationType.TemporaryTopic;
        IDestination first = await session.GetDestinationAsync(name, firstKind, cancellationToken);
        IDestination second = await session.GetDestinationAsync(name, secondKind, cancellationToken);
        IDestination queue = topicFirst ? second : first;
        IDestination topic = topicFirst ? first : second;

        Assert.IsAssignableFrom<ITemporaryQueue>(queue);
        Assert.IsAssignableFrom<ITemporaryTopic>(topic);
        Assert.NotSame(queue, topic);
        Assert.Same(queue, await session.GetDestinationAsync(name, DestinationType.TemporaryQueue, cancellationToken));
        Assert.Same(topic, await session.GetDestinationAsync(name, DestinationType.TemporaryTopic, cancellationToken));
        Assert.Equal(2, created);
        Assert.Same(queue, session.GetTemporaryDestination(name, DestinationType.TemporaryQueue));
        Assert.Same(topic, session.GetTemporaryDestination(name, DestinationType.TemporaryTopic));

        if (failFirstDelete)
        {
            NMSException actual = await Assert.ThrowsAsync<NMSException>(() => session.DeleteQueueAsync(name, cancellationToken));
            Assert.Same(deleteFailure, actual);
            Assert.Same(queue, await session.GetDestinationAsync(name, DestinationType.TemporaryQueue, cancellationToken));
            Assert.Same(topic, await session.GetDestinationAsync(name, DestinationType.TemporaryTopic, cancellationToken));
            failDelete = false;
        }

        await session.DeleteQueueAsync(name, cancellationToken);
        Assert.Same(topic, await session.GetDestinationAsync(name, DestinationType.TemporaryTopic, cancellationToken));
        Assert.All(deleted, destination => Assert.Same(queue, destination));
        Assert.Equal(failFirstDelete ? 2 : 1, deleted.Count);
        IDestination replacementQueue = await session.GetDestinationAsync(name, DestinationType.TemporaryQueue, cancellationToken);
        Assert.NotSame(queue, replacementQueue);
        Assert.Equal(3, created);
        await session.DeleteTopicAsync(name, cancellationToken);
        Assert.Same(topic, deleted[^1]);
        Assert.Same(replacementQueue, await session.GetDestinationAsync(name, DestinationType.TemporaryQueue, cancellationToken));
        IDestination replacementTopic = await session.GetDestinationAsync(name, DestinationType.TemporaryTopic, cancellationToken);
        Assert.NotSame(topic, replacementTopic);
        Assert.Equal(4, created);

        object? Delete(IDestination destination)
        {
            deleted.Add(destination);
            if (failDelete)
                throw deleteFailure;
            return null;
        }
    }

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
        Assert.True(connection.TryGetTemporaryEntity("shared", topic ? DestinationType.TemporaryTopic : DestinationType.TemporaryQueue, out IDestination? registered));
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
        Assert.Same(sendDestination, sender.GetTemporaryDestination(logicalName, kind));
        Assert.Same(sendDestination, receiver.GetTemporaryDestination(logicalName, kind));
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
