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
    public async Task AcyclicBindingGraph_ConnectsEveryDeclaredDestinationAsync()
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
            await fabric.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-FABRIC-BINDING", "cycle-rejected-without-partial-edge")]
    public async Task CyclicBinding_IsRejectedWithoutChangingTheGraphAsync()
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
            await fabric.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-BACKPRESSURE", "immediate-queue-capacity-and-fifo")]
    public async Task ImmediateQueueCapacity_BlocksTheNextProducerAndPreservesFifoDeliveryAsync()
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
            await queue.DeliverAsync(new FabricDelivery("first", cancellationToken), TestContext.Current.CancellationToken);
            await receiver.FirstDeliveryStarted.WaitAsync(timeout, cancellationToken);
            await queue.DeliverAsync(new FabricDelivery("second", cancellationToken), TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

            Task thirdAdmission = queue.DeliverAsync(new FabricDelivery("third", cancellationToken), TestContext.Current.CancellationToken);
            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
            Assert.False(thirdAdmission.IsCompleted);

            receiver.ReleaseFirstDelivery();
            await thirdAdmission.WaitAsync(timeout, cancellationToken);
            await receiver.Completed.WaitAsync(timeout, cancellationToken);

            Assert.Equal(["first", "second", "third"], receiver.Values);
        }
        finally
        {
            await fabric.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-BACKPRESSURE", "scheduled-admission-capacity")]
    public async Task ScheduledDeliveryCapacity_BlocksUntilTheOwnedDelayCompletesAsync()
    {
        TimeSpan timeout = TimeSpan.FromSeconds(10);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var fabric = new MessageFabric<object, FabricMessage>(queueCapacity: 1);
        var context = new object();
        IMessageQueue<object, FabricMessage> queue = fabric.GetQueue(context, "bounded-scheduled");
        var receiver = new RecordingReceiver(expectedCount: 2);
        queue.ConnectMessageReceiver(context, receiver);
        var delayProvider = Assert.IsType<InMemoryDelayProvider>(fabric.DelayProvider);
        DateTimeOffset enqueueTime = delayProvider.UtcNow.AddMinutes(1);

        try
        {
            await queue.DeliverAsync(new FabricDelivery("first", cancellationToken, enqueueTime), TestContext.Current.CancellationToken);
            Task secondAdmission = queue.DeliverAsync(new FabricDelivery("second", cancellationToken, enqueueTime), TestContext.Current.CancellationToken);

            Assert.False(secondAdmission.IsCompleted);

            delayProvider.Advance(TimeSpan.FromMinutes(1));
            await secondAdmission.WaitAsync(timeout, cancellationToken);
            await receiver.Completed.WaitAsync(timeout, cancellationToken);

            Assert.Equal(["first", "second"], receiver.Values);
        }
        finally
        {
            await fabric.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-BACKPRESSURE", "scheduled-admission-cancellation")]
    public async Task ScheduledCapacityWait_PreservesTheCanceledProducerTokenAndAddsNoDeliveryAsync()
    {
        TimeSpan timeout = TimeSpan.FromSeconds(10);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var fabric = new MessageFabric<object, FabricMessage>(queueCapacity: 1);
        var context = new object();
        IMessageQueue<object, FabricMessage> queue = fabric.GetQueue(context, "bounded-scheduled-cancellation");
        var receiver = new RecordingReceiver(expectedCount: 1);
        queue.ConnectMessageReceiver(context, receiver);
        var delayProvider = Assert.IsType<InMemoryDelayProvider>(fabric.DelayProvider);
        DateTimeOffset enqueueTime = delayProvider.UtcNow.AddMinutes(1);
        using var source = new CancellationTokenSource();

        try
        {
            await queue.DeliverAsync(new FabricDelivery("accepted", cancellationToken, enqueueTime), TestContext.Current.CancellationToken);
            Task blocked = queue.DeliverAsync(new FabricDelivery("canceled", source.Token, enqueueTime), TestContext.Current.CancellationToken);
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
            await fabric.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-BACKGROUND-OWNERSHIP", "stop-awaits-scheduled-delivery")]
    public async Task QueueStop_CancelsAndAwaitsEveryAcceptedScheduledDeliveryAsync()
    {
        TimeSpan timeout = TimeSpan.FromSeconds(10);
        var delayProvider = new ControlledDelayProvider();
        var queue = new MessageQueue<object, FabricMessage>(
            new PassiveFabricObserver(),
            "owned-scheduled-delivery",
            delayProvider,
            capacity: 1);
        await queue.DeliverAsync(new FabricDelivery(
            "scheduled",
            TestContext.Current.CancellationToken,
            delayProvider.UtcNow.AddMinutes(1)), TestContext.Current.CancellationToken);
        await delayProvider.Waiting.WaitAsync(timeout, TestContext.Current.CancellationToken);

        Task stop = queue.StopAsync(CancellationToken.None);
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
        DateTimeOffset? enqueueTime = null) : DeliveryContext<FabricMessage>
    {
        private readonly HashSet<IMessageSink<FabricMessage>> _delivered = [];

        public CancellationToken CancellationToken { get; } = cancellationToken;
        public FabricMessage Message { get; } = new(value);
        public string? RoutingKey => null;
        public DateTimeOffset? EnqueueTime { get; } = enqueueTime;
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

        public Task DeliverAsync(FabricMessage message, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); _values.Enqueue(message.Value);
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

        public async Task DeliverAsync(FabricMessage message, CancellationToken cancellationToken)
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

        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default) =>
            DelayAsync(UtcNow.Add(delay), cancellationToken);

        public Task DelayAsync(DateTimeOffset delayUntil, CancellationToken cancellationToken = default)
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
