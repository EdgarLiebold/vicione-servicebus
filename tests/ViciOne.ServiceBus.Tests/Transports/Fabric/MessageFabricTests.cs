using System.Collections.Concurrent;
using ViciOne.ServiceBus.InMemoryTransport;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports.Fabric;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transports.Fabric;

public sealed class MessageFabricTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-FABRIC-BINDING", "acyclic-exchange-and-queue-graph")]
    public async Task AcyclicBindingGraph_ConnectsEveryDeclaredDestination()
    {
        var fabric = new MessageFabric<object, FabricMessage>();
        var context = new object();

        try
        {
            fabric.ExchangeBind(context, "Namespace.A", "input-exchange", null!);
            fabric.ExchangeBind(context, "Namespace.B", "input-exchange", null!);
            fabric.QueueBind(context, "input-exchange", "input-queue");

            IMessageExchange<FabricMessage> namespaceA = fabric.GetExchange(context, "Namespace.A", ExchangeType.FanOut);
            IMessageExchange<FabricMessage> namespaceB = fabric.GetExchange(context, "Namespace.B", ExchangeType.FanOut);
            IMessageExchange<FabricMessage> inputExchange = fabric.GetExchange(context, "input-exchange", ExchangeType.FanOut);
            IMessageQueue<object, FabricMessage> inputQueue = fabric.GetQueue(context, "input-queue");

            Assert.Same(inputExchange, Assert.Single(namespaceA.Sinks));
            Assert.Same(inputExchange, Assert.Single(namespaceB.Sinks));
            Assert.Same(inputQueue, Assert.Single(inputExchange.Sinks));
            Assert.Equal("input-queue", inputQueue.Name);
        }
        finally
        {
            await fabric.Stop(CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-FABRIC-BINDING", "cycle-rejected-without-partial-edge")]
    public async Task CyclicBinding_IsRejectedWithoutChangingTheGraph()
    {
        var fabric = new MessageFabric<object, FabricMessage>();
        var context = new object();

        try
        {
            fabric.ExchangeBind(context, "Namespace.A", "input-exchange", null!);
            fabric.ExchangeBind(context, "Namespace.B", "input-exchange", null!);
            fabric.ExchangeBind(context, "input-exchange", "output-exchange", null!);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                fabric.ExchangeBind(context, "output-exchange", "Namespace.A", null!));

            Assert.Equal("The exchange binding would create a cycle in the messaging fabric.", exception.Message);
            IMessageExchange<FabricMessage> namespaceA = fabric.GetExchange(context, "Namespace.A", ExchangeType.FanOut);
            IMessageExchange<FabricMessage> inputExchange = fabric.GetExchange(context, "input-exchange", ExchangeType.FanOut);
            IMessageExchange<FabricMessage> outputExchange = fabric.GetExchange(context, "output-exchange", ExchangeType.FanOut);
            Assert.Same(inputExchange, Assert.Single(namespaceA.Sinks));
            Assert.Same(outputExchange, Assert.Single(inputExchange.Sinks));
            Assert.Empty(outputExchange.Sinks);
        }
        finally
        {
            await fabric.Stop(CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-BACKPRESSURE", "immediate-queue-capacity-and-fifo")]
    public async Task ImmediateQueueCapacity_BlocksTheNextProducerAndPreservesFifoDelivery()
    {
        TimeSpan timeout = TimeSpan.FromSeconds(10);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var fabric = new MessageFabric<object, FabricMessage>(queueCapacity: 1);
        var context = new object();
        IMessageQueue<object, FabricMessage> queue = fabric.GetQueue(context, "bounded-immediate");
        var receiver = new BlockingRecordingReceiver(expectedCount: 3);
        queue.ConnectMessageReceiver(context, receiver);

        try
        {
            await queue.Deliver(new FabricDelivery("first", cancellationToken));
            await receiver.FirstDeliveryStarted.WaitAsync(timeout, cancellationToken);
            await queue.Deliver(new FabricDelivery("second", cancellationToken)).WaitAsync(timeout, cancellationToken);

            Task thirdAdmission = queue.Deliver(new FabricDelivery("third", cancellationToken));
            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
            Assert.False(thirdAdmission.IsCompleted);

            receiver.ReleaseFirstDelivery();
            await thirdAdmission.WaitAsync(timeout, cancellationToken);
            await receiver.Completed.WaitAsync(timeout, cancellationToken);

            Assert.Equal(["first", "second", "third"], receiver.Values);
        }
        finally
        {
            await fabric.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-BACKPRESSURE", "scheduled-admission-capacity")]
    public async Task ScheduledDeliveryCapacity_BlocksUntilTheOwnedDelayCompletes()
    {
        TimeSpan timeout = TimeSpan.FromSeconds(10);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var fabric = new MessageFabric<object, FabricMessage>(queueCapacity: 1);
        var context = new object();
        IMessageQueue<object, FabricMessage> queue = fabric.GetQueue(context, "bounded-scheduled");
        var receiver = new RecordingReceiver(expectedCount: 2);
        queue.ConnectMessageReceiver(context, receiver);
        var delayProvider = Assert.IsType<InMemoryDelayProvider>(fabric.DelayProvider);
        DateTime enqueueTime = delayProvider.UtcNow.AddMinutes(1).UtcDateTime;

        try
        {
            await queue.Deliver(new FabricDelivery("first", cancellationToken, enqueueTime));
            Task secondAdmission = queue.Deliver(new FabricDelivery("second", cancellationToken, enqueueTime));

            Assert.False(secondAdmission.IsCompleted);

            delayProvider.Advance(TimeSpan.FromMinutes(1));
            await secondAdmission.WaitAsync(timeout, cancellationToken);
            await receiver.Completed.WaitAsync(timeout, cancellationToken);

            Assert.Equal(["first", "second"], receiver.Values);
        }
        finally
        {
            await fabric.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-BACKPRESSURE", "scheduled-admission-cancellation")]
    public async Task ScheduledCapacityWait_PreservesTheCanceledProducerTokenAndAddsNoDelivery()
    {
        TimeSpan timeout = TimeSpan.FromSeconds(10);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var fabric = new MessageFabric<object, FabricMessage>(queueCapacity: 1);
        var context = new object();
        IMessageQueue<object, FabricMessage> queue = fabric.GetQueue(context, "bounded-scheduled-cancellation");
        var receiver = new RecordingReceiver(expectedCount: 1);
        queue.ConnectMessageReceiver(context, receiver);
        var delayProvider = Assert.IsType<InMemoryDelayProvider>(fabric.DelayProvider);
        DateTime enqueueTime = delayProvider.UtcNow.AddMinutes(1).UtcDateTime;
        using var source = new CancellationTokenSource();

        try
        {
            await queue.Deliver(new FabricDelivery("accepted", cancellationToken, enqueueTime));
            Task blocked = queue.Deliver(new FabricDelivery("canceled", source.Token, enqueueTime));
            Assert.False(blocked.IsCompleted);

            source.Cancel();
            OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => blocked);
            delayProvider.Advance(TimeSpan.FromMinutes(1));
            await receiver.Completed.WaitAsync(timeout, cancellationToken);

            Assert.Equal(source.Token, actual.CancellationToken);
            Assert.Equal(["accepted"], receiver.Values);
        }
        finally
        {
            await fabric.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-BACKGROUND-OWNERSHIP", "stop-awaits-scheduled-delivery")]
    public async Task QueueStop_CancelsAndAwaitsEveryAcceptedScheduledDelivery()
    {
        TimeSpan timeout = TimeSpan.FromSeconds(10);
        var delayProvider = new ControlledDelayProvider();
        var queue = new MessageQueue<object, FabricMessage>(
            new PassiveFabricObserver(),
            "owned-scheduled-delivery",
            delayProvider,
            capacity: 1);
        await queue.Deliver(new FabricDelivery(
            "scheduled",
            TestContext.Current.CancellationToken,
            delayProvider.UtcNow.AddMinutes(1).UtcDateTime));
        await delayProvider.Waiting.WaitAsync(timeout, TestContext.Current.CancellationToken);

        Task stop = queue.Stop(CancellationToken.None);
        await delayProvider.CancellationRequested.WaitAsync(timeout, TestContext.Current.CancellationToken);

        await Task.Delay(TimeSpan.FromMilliseconds(250), TestContext.Current.CancellationToken);
        Assert.False(stop.IsCompleted);

        delayProvider.CompleteCancellation();
        await stop.WaitAsync(timeout, CancellationToken.None);

        Assert.True(stop.IsCompletedSuccessfully);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-BACKPRESSURE", "positive-configured-capacity")]
    public void NonPositiveCapacity_IsRejectedByTheFabricAndPublicHostConfiguration()
    {
        ArgumentOutOfRangeException direct = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MessageFabric<object, FabricMessage>(queueCapacity: 0));
        ArgumentOutOfRangeException configured = Assert.Throws<ArgumentOutOfRangeException>(() =>
            Bus.Factory.CreateUsingInMemory(configuration => configuration.Host(host => host.QueueCapacity = -1)));

        Assert.Equal("queueCapacity", direct.ParamName);
        Assert.Equal(0, direct.ActualValue);
        Assert.Equal("value", configured.ParamName);
        Assert.Equal(-1, configured.ActualValue);
    }

    private sealed record FabricMessage(string Value);

    private sealed class FabricDelivery(
        string value,
        CancellationToken cancellationToken,
        DateTime? enqueueTime = null) : DeliveryContext<FabricMessage>
    {
        private readonly HashSet<IMessageSink<FabricMessage>> _delivered = [];

        public CancellationToken CancellationToken { get; } = cancellationToken;
        public FabricMessage Message { get; } = new(value);
        public string? RoutingKey => null;
        public DateTime? EnqueueTime { get; } = enqueueTime;
        public long? ReceiverId => null;

        public bool WasAlreadyDelivered(IMessageSink<FabricMessage> sink) => _delivered.Contains(sink);

        public void Delivered(IMessageSink<FabricMessage> sink) => _delivered.Add(sink);
    }

    private sealed class RecordingReceiver(int expectedCount) : IMessageReceiver<FabricMessage>
    {
        private readonly TaskCompletionSource _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly ConcurrentQueue<string> _values = new();
        private int _received;

        public Task Completed => _completed.Task;
        public string[] Values => _values.ToArray();

        public Task Deliver(FabricMessage message, CancellationToken cancellationToken)
        {
            _values.Enqueue(message.Value);
            if (Interlocked.Increment(ref _received) == expectedCount)
                _completed.TrySetResult();

            return Task.CompletedTask;
        }

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class BlockingRecordingReceiver(int expectedCount) : IMessageReceiver<FabricMessage>
    {
        private readonly TaskCompletionSource _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _firstDeliveryRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _firstDeliveryStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly ConcurrentQueue<string> _values = new();
        private int _received;

        public Task Completed => _completed.Task;
        public Task FirstDeliveryStarted => _firstDeliveryStarted.Task;
        public string[] Values => _values.ToArray();

        public async Task Deliver(FabricMessage message, CancellationToken cancellationToken)
        {
            _values.Enqueue(message.Value);
            int received = Interlocked.Increment(ref _received);
            if (received == 1)
            {
                _firstDeliveryStarted.TrySetResult();
                await _firstDeliveryRelease.Task.WaitAsync(cancellationToken);
            }

            if (received == expectedCount)
                _completed.TrySetResult();
        }

        public void Probe(ProbeContext context)
        {
        }

        public void ReleaseFirstDelivery()
        {
            _firstDeliveryRelease.TrySetResult();
        }
    }

    private sealed class ControlledDelayProvider : IInMemoryDelayProvider
    {
        private readonly TaskCompletionSource _cancellationRequested =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _delay = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _waiting = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private CancellationToken _cancellationToken;

        public Task CancellationRequested => _cancellationRequested.Task;
        public DateTimeOffset UtcNow { get; } = new(2026, 9, 3, 10, 0, 0, TimeSpan.Zero);
        public Task Waiting => _waiting.Task;

        public Task Delay(TimeSpan delay, CancellationToken cancellationToken = default) =>
            Delay(UtcNow.Add(delay), cancellationToken);

        public Task Delay(DateTimeOffset delayUntil, CancellationToken cancellationToken = default)
        {
            _cancellationToken = cancellationToken;
            cancellationToken.Register(() => _cancellationRequested.TrySetResult());
            _waiting.TrySetResult();
            return _delay.Task;
        }

        public void Advance(TimeSpan duration)
        {
            throw new InvalidOperationException("This controlled delay advances only through its explicit test completion.");
        }

        public void CompleteCancellation()
        {
            _delay.TrySetCanceled(_cancellationToken);
        }
    }

    private sealed class PassiveFabricObserver : IMessageFabricObserver<object>
    {
        public void ExchangeDeclared(object context, string name, ExchangeType exchangeType)
        {
        }

        public void ExchangeBindingCreated(object context, string source, string destination, string? routingKey = null)
        {
        }

        public void QueueDeclared(object context, string name)
        {
        }

        public void QueueBindingCreated(object context, string source, string destination)
        {
        }

        public TopologyHandle ConsumerConnected(object context, TopologyHandle handle, string queueName) => handle;
    }
}

public sealed class MessageTopicExchangeTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TOPIC-EXCHANGE-ROUTING", "hash-pattern")]
    public async Task HashPattern_DeliversTheRoutedMessage()
    {
        var exchange = new MessageTopicExchange<TopicMessage>("test-exchange");
        var sink = new RecordingSink();
        using ConnectHandle connection = exchange.Connect(sink, "#");

        Delivery delivery = await Deliver(exchange, "alpha", "matching");

        Assert.Equal(new[] { new ReceivedMessage("alpha", "matching") }, sink.Messages);
        Assert.True(delivery.WasAlreadyDelivered(sink));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPIC-EXCHANGE-ROUTING", "single-segment-wildcard")]
    public async Task SingleSegmentWildcard_DeliversOnlyTheMatchingRoute()
    {
        var exchange = new MessageTopicExchange<TopicMessage>("test-exchange");
        var sink = new RecordingSink();
        using ConnectHandle connection = exchange.Connect(sink, "car.*");

        await Deliver(exchange, "bus.red", "bad-red");
        await Deliver(exchange, "bus.green", "bad-green");
        Delivery matchingDelivery = await Deliver(exchange, "car.blue", "good");

        Assert.Equal(new[] { new ReceivedMessage("car.blue", "good") }, sink.Messages);
        Assert.True(matchingDelivery.WasAlreadyDelivered(sink));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPIC-EXCHANGE-ROUTING", "three-segment-wildcard")]
    public async Task ThreeSegmentWildcard_DeliversOnlyTheMatchingRoute()
    {
        var exchange = new MessageTopicExchange<TopicMessage>("test-exchange");
        var sink = new RecordingSink();
        using ConnectHandle connection = exchange.Connect(sink, "car.*.large");

        await Deliver(exchange, "bus.red.large", "bad-bus-large");
        await Deliver(exchange, "car.green.small", "bad-car-small");
        await Deliver(exchange, "bus.green.small", "bad-bus-small");
        Delivery matchingDelivery = await Deliver(exchange, "car.blue.large", "good");

        Assert.Equal(new[] { new ReceivedMessage("car.blue.large", "good") }, sink.Messages);
        Assert.True(matchingDelivery.WasAlreadyDelivered(sink));
    }

    private static async Task<Delivery> Deliver(MessageTopicExchange<TopicMessage> exchange, string routingKey, string value)
    {
        var delivery = new Delivery(new TopicMessage(value), routingKey, TestContext.Current.CancellationToken);
        await exchange.Deliver(delivery);
        return delivery;
    }

    private sealed class RecordingSink :
        IMessageSink<TopicMessage>
    {
        public List<ReceivedMessage> Messages { get; } = [];

        public Task Deliver(DeliveryContext<TopicMessage> context)
        {
            Messages.Add(new ReceivedMessage(context.RoutingKey!, context.Message.Value));
            return Task.CompletedTask;
        }

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class Delivery :
        DeliveryContext<TopicMessage>
    {
        private readonly HashSet<IMessageSink<TopicMessage>> _delivered = [];

        public Delivery(TopicMessage message, string routingKey, CancellationToken cancellationToken)
        {
            Message = message;
            RoutingKey = routingKey;
            CancellationToken = cancellationToken;
        }

        public CancellationToken CancellationToken { get; }
        public TopicMessage Message { get; }
        public string RoutingKey { get; }
        public DateTime? EnqueueTime => null;
        public long? ReceiverId => null;

        public bool WasAlreadyDelivered(IMessageSink<TopicMessage> sink) => _delivered.Contains(sink);

        public void Delivered(IMessageSink<TopicMessage> sink) => _delivered.Add(sink);
    }

    private sealed record TopicMessage(string Value);
    private sealed record ReceivedMessage(string RoutingKey, string Value);
}
