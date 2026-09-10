using System.Collections.Concurrent;
using ViciOne.ServiceBus.Providers.Transports;
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
        var fabric = new MessageFabric<FabricMessage>();

        try
        {
            fabric.ExchangeBind("Namespace.A", "input-exchange", null);
            fabric.ExchangeBind("Namespace.B", "input-exchange", null);
            fabric.QueueBind("input-exchange", "input-queue");

            IMessageExchange<FabricMessage> namespaceA = fabric.GetExchange("Namespace.A", InMemoryExchangeType.FanOut);
            IMessageExchange<FabricMessage> namespaceB = fabric.GetExchange("Namespace.B", InMemoryExchangeType.FanOut);
            IMessageExchange<FabricMessage> inputExchange = fabric.GetExchange("input-exchange", InMemoryExchangeType.FanOut);
            IMessageQueue<FabricMessage> inputQueue = fabric.GetQueue("input-queue");

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
        var fabric = new MessageFabric<FabricMessage>();

        try
        {
            fabric.ExchangeBind("Namespace.A", "input-exchange", null);
            fabric.ExchangeBind("Namespace.B", "input-exchange", null);
            fabric.ExchangeBind("input-exchange", "output-exchange", null);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                fabric.ExchangeBind("output-exchange", "Namespace.A", null));

            Assert.Equal("The exchange binding would create a cycle in the message fabric.", exception.Message);
            IMessageExchange<FabricMessage> namespaceA = fabric.GetExchange("Namespace.A", InMemoryExchangeType.FanOut);
            IMessageExchange<FabricMessage> inputExchange = fabric.GetExchange("input-exchange", InMemoryExchangeType.FanOut);
            IMessageExchange<FabricMessage> outputExchange = fabric.GetExchange("output-exchange", InMemoryExchangeType.FanOut);
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
    [RequirementCoverage("REQ-VSB-MESSAGE-FABRIC-BINDING", "declaration-contract-and-routing-type-consistency")]
    public async Task ExchangeDeclaration_RejectsInvalidInputsAndConflictingRoutingBehaviorAsync()
    {
        var fabric = new MessageFabric<FabricMessage>();

        try
        {
            fabric.ExchangeDeclare("orders", InMemoryExchangeType.Direct);

            InvalidOperationException conflict = Assert.Throws<InvalidOperationException>(() =>
                fabric.ExchangeDeclare("ORDERS", InMemoryExchangeType.Topic));
            ArgumentOutOfRangeException unsupported = Assert.Throws<ArgumentOutOfRangeException>(() =>
                fabric.ExchangeDeclare("unsupported", (InMemoryExchangeType)42));
            fabric.ExchangeDeclare("topic", InMemoryExchangeType.Topic);
            fabric.QueueDeclare("declared-queue");

            Assert.Contains("already declared as Direct", conflict.Message, StringComparison.Ordinal);
            Assert.Equal("exchangeType", unsupported.ParamName);
            Assert.Equal(InMemoryExchangeType.Topic, fabric.GetExchange("topic", InMemoryExchangeType.Topic).ExchangeType);
            Assert.Equal("declared-queue", fabric.GetQueue("declared-queue").Name);
            Assert.Equal("name", Assert.Throws<ArgumentException>(() =>
                fabric.ExchangeDeclare(" ", InMemoryExchangeType.Direct)).ParamName);
            Assert.Equal("name", Assert.Throws<ArgumentException>(() => fabric.QueueDeclare(" ")).ParamName);
            Assert.Equal("name", Assert.Throws<ArgumentException>(() => fabric.GetQueue(" ")).ParamName);
            Assert.Equal("source", Assert.Throws<ArgumentException>(() =>
                fabric.ExchangeBind(" ", "destination", null)).ParamName);
            Assert.Equal("destination", Assert.Throws<ArgumentException>(() =>
                fabric.ExchangeBind("source", " ", null)).ParamName);
            Assert.Equal("source", Assert.Throws<ArgumentException>(() =>
                fabric.QueueBind(" ", "queue")).ParamName);
            Assert.Equal("destination", Assert.Throws<ArgumentException>(() =>
                fabric.QueueBind("source", " ")).ParamName);
            Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => fabric.Probe(null!)).ParamName);
            Assert.NotNull(fabric.GetProbeResult(TestContext.Current.CancellationToken));
        }
        finally
        {
            await fabric.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-FABRIC-BINDING", "case-insensitive-idempotent-bindings")]
    public async Task RepeatedBindings_AreIdempotentAndEntityNamesAreCaseInsensitiveAsync()
    {
        var fabric = new MessageFabric<FabricMessage>();

        try
        {
            fabric.ExchangeBind("source", "destination", null);
            fabric.ExchangeBind("SOURCE", "DESTINATION", null);
            fabric.ExchangeBind("source", "destination", "ignored-by-fan-out");
            fabric.QueueBind("destination", "queue");
            fabric.QueueBind("DESTINATION", "QUEUE");

            fabric.ExchangeDeclare("direct", InMemoryExchangeType.Direct);
            fabric.ExchangeBind("direct", "direct-destination", null);
            fabric.ExchangeBind("DIRECT", "DIRECT-DESTINATION", string.Empty);

            IMessageExchange<FabricMessage> source = fabric.GetExchange(
                "Source",
                InMemoryExchangeType.FanOut);
            IMessageExchange<FabricMessage> destination = fabric.GetExchange(
                "Destination",
                InMemoryExchangeType.FanOut);

            Assert.Single(source.Sinks);
            Assert.Single(destination.Sinks);
            Assert.Single(fabric.GetExchange("direct", InMemoryExchangeType.Direct).Sinks);
            Assert.Equal("destination", Assert.Throws<ArgumentException>(() =>
                fabric.ExchangeBind("same", "SAME", null)).ParamName);
        }
        finally
        {
            await fabric.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-FABRIC-BINDING", "diamond-topology-single-delivery")]
    public async Task DiamondTopology_DeliversToTheSharedDestinationExactlyOnceAsync()
    {
        var fabric = new MessageFabric<FabricMessage>();
        var sink = new RecordingSink();

        try
        {
            fabric.ExchangeBind("root", "left", null);
            fabric.ExchangeBind("root", "right", null);
            fabric.ExchangeBind("left", "destination", null);
            fabric.ExchangeBind("right", "destination", null);
            IMessageExchange<FabricMessage> root = fabric.GetExchange("root", InMemoryExchangeType.FanOut);
            IMessageExchange<FabricMessage> destination = fabric.GetExchange(
                "destination",
                InMemoryExchangeType.FanOut);
            using ConnectHandle connection = destination.Connect(sink, null);
            var delivery = new FabricDelivery("message", TestContext.Current.CancellationToken);

            await root.DeliverAsync(delivery, TestContext.Current.CancellationToken);

            Assert.Equal(["message"], sink.Values);
            Assert.True(delivery.IsReserved(destination));
            Assert.True(delivery.IsReserved(sink));
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
        var fabric = new MessageFabric<FabricMessage>(queueCapacity: 1);
        IMessageQueue<FabricMessage> queue = fabric.GetQueue("bounded-immediate");
        var receiver = new BlockingRecordingReceiver(expectedCount: 3);
        queue.ConnectMessageReceiver(receiver);

        try
        {
            Assert.Equal("receiver", Assert.Throws<ArgumentNullException>(() => queue.ConnectMessageReceiver(null!)).ParamName);
            Assert.Equal("context", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
                queue.DeliverAsync(null!, TestContext.Current.CancellationToken))).ParamName);
            Assert.NotNull(queue.GetProbeResult(TestContext.Current.CancellationToken));

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
        var fabric = new MessageFabric<FabricMessage>(queueCapacity: 1);
        IMessageQueue<FabricMessage> queue = fabric.GetQueue("bounded-scheduled");
        var receiver = new RecordingReceiver(expectedCount: 2);
        queue.ConnectMessageReceiver(receiver);
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
        var fabric = new MessageFabric<FabricMessage>(queueCapacity: 1);
        IMessageQueue<FabricMessage> queue = fabric.GetQueue("bounded-scheduled-cancellation");
        var receiver = new RecordingReceiver(expectedCount: 1);
        queue.ConnectMessageReceiver(receiver);
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
    [RequirementCoverage("REQ-VSB-INMEMORY-BACKPRESSURE", "scheduled-operation-cancellation")]
    public async Task ScheduledCapacityWait_HonorsAndPreservesTheOperationTokenAsync()
    {
        TimeSpan timeout = TimeSpan.FromSeconds(10);
        var fabric = new MessageFabric<FabricMessage>(queueCapacity: 1);
        IMessageQueue<FabricMessage> queue = fabric.GetQueue("operation-cancellation");
        var receiver = new RecordingReceiver(expectedCount: 1);
        queue.ConnectMessageReceiver(receiver);
        var delayProvider = Assert.IsType<InMemoryDelayProvider>(fabric.DelayProvider);
        DateTimeOffset enqueueTime = delayProvider.UtcNow.AddMinutes(1);
        using var operation = new CancellationTokenSource();

        try
        {
            await queue.DeliverAsync(
                new FabricDelivery("accepted", TestContext.Current.CancellationToken, enqueueTime),
                TestContext.Current.CancellationToken);
            Task blocked = queue.DeliverAsync(
                new FabricDelivery("canceled", TestContext.Current.CancellationToken, enqueueTime),
                operation.Token);

            operation.Cancel();
            OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => blocked);
            delayProvider.Advance(TimeSpan.FromMinutes(1));
            await receiver.Completed.WaitAsync(timeout, TestContext.Current.CancellationToken);

            Assert.Equal(operation.Token, actual.CancellationToken);
            Assert.Equal(["accepted"], receiver.Values);
        }
        finally
        {
            await fabric.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-BACKPRESSURE", "dispatcher-continues-after-receiver-fault")]
    public async Task QueueDispatcher_ContinuesAfterAReceiverFaultAsync()
    {
        TimeSpan timeout = TimeSpan.FromSeconds(10);
        var fabric = new MessageFabric<FabricMessage>();
        IMessageQueue<FabricMessage> queue = fabric.GetQueue("receiver-fault");
        var receiver = new FailOnceReceiver();
        queue.ConnectMessageReceiver(receiver);

        try
        {
            await queue.DeliverAsync(
                new FabricDelivery("fault", TestContext.Current.CancellationToken),
                TestContext.Current.CancellationToken);
            await queue.DeliverAsync(
                new FabricDelivery("success", TestContext.Current.CancellationToken),
                TestContext.Current.CancellationToken);
            await receiver.Completed.WaitAsync(timeout, TestContext.Current.CancellationToken);

            Assert.Equal(2, receiver.Attempts);
            Assert.Equal(["success"], receiver.Values);
        }
        finally
        {
            await fabric.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-BACKPRESSURE", "canceled-dispatch-does-not-reach-receiver")]
    public async Task CanceledMessageWaitingForAReceiver_IsDiscardedBeforeDispatchAsync()
    {
        TimeSpan timeout = TimeSpan.FromSeconds(10);
        var fabric = new MessageFabric<FabricMessage>();
        IMessageQueue<FabricMessage> queue = fabric.GetQueue("canceled-dispatch");
        using var canceled = new CancellationTokenSource();

        try
        {
            await queue.DeliverAsync(
                new FabricDelivery("canceled", canceled.Token),
                TestContext.Current.CancellationToken);
            canceled.Cancel();

            var receiver = new RecordingReceiver(expectedCount: 1);
            queue.ConnectMessageReceiver(receiver);
            await queue.DeliverAsync(
                new FabricDelivery("delivered", TestContext.Current.CancellationToken),
                TestContext.Current.CancellationToken);
            await receiver.Completed.WaitAsync(timeout, TestContext.Current.CancellationToken);

            Assert.Equal(["delivered"], receiver.Values);
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
        var queue = new MessageQueue<FabricMessage>(
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
    [RequirementCoverage("REQ-VSB-INMEMORY-BACKGROUND-OWNERSHIP", "stop-cancels-blocked-admission-and-drains-buffer")]
    public async Task QueueStop_CancelsBlockedAdmissionAndCompletesWithBufferedMessagesAsync()
    {
        TimeSpan timeout = TimeSpan.FromSeconds(10);
        var delayProvider = new InMemoryDelayProvider();
        var queue = new MessageQueue<FabricMessage>("stop-with-buffered-messages", delayProvider, capacity: 1);
        var receiver = new BlockingRecordingReceiver(expectedCount: 3);
        queue.ConnectMessageReceiver(receiver);

        await queue.DeliverAsync(
            new FabricDelivery("active", TestContext.Current.CancellationToken),
            TestContext.Current.CancellationToken);
        await receiver.FirstDeliveryStarted.WaitAsync(timeout, TestContext.Current.CancellationToken);
        await queue.DeliverAsync(
            new FabricDelivery("buffered", TestContext.Current.CancellationToken),
            TestContext.Current.CancellationToken);
        Task blockedAdmission = queue.DeliverAsync(
            new FabricDelivery("blocked", TestContext.Current.CancellationToken),
            TestContext.Current.CancellationToken);

        Assert.False(blockedAdmission.IsCompleted);

        Task stop = queue.StopAsync(CancellationToken.None);
        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => blockedAdmission);
        await stop.WaitAsync(timeout, CancellationToken.None);

        Assert.Equal(queue.Stopping, exception.CancellationToken);
        Assert.True(stop.IsCompletedSuccessfully);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-BACKPRESSURE", "positive-configured-capacity")]
    public void NonPositiveCapacity_IsRejectedByTheFabricAndPublicHostConfiguration()
    {
        var delayProvider = new InMemoryDelayProvider();
        ArgumentOutOfRangeException direct = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MessageFabric<FabricMessage>(queueCapacity: 0));
        ArgumentOutOfRangeException configured = Assert.Throws<ArgumentOutOfRangeException>(() =>
            Bus.Factory.CreateUsingInMemory(configuration => configuration.Host(host => host.QueueCapacity = -1)));

        Assert.Equal("queueCapacity", direct.ParamName);
        Assert.Equal(0, direct.ActualValue);
        Assert.Equal("value", configured.ParamName);
        Assert.Equal(-1, configured.ActualValue);
        Assert.Equal("name", Assert.Throws<ArgumentException>(() =>
            new MessageQueue<FabricMessage>(" ", delayProvider)).ParamName);
        Assert.Equal("delayProvider", Assert.Throws<ArgumentNullException>(() =>
            new MessageQueue<FabricMessage>("queue", null!)).ParamName);
        Assert.Equal("capacity", Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MessageQueue<FabricMessage>("queue", delayProvider, 0)).ParamName);
    }

    private sealed record FabricMessage(string Value);

    private sealed class FabricDelivery(
        string value,
        CancellationToken cancellationToken,
        DateTimeOffset? enqueueTime = null) : IMessageDeliveryContext<FabricMessage>
    {
        private readonly HashSet<IMessageSink<FabricMessage>> _delivered = [];

        public CancellationToken CancellationToken { get; } = cancellationToken;
        public FabricMessage Message { get; } = new(value);
        public string? RoutingKey => null;
        public DateTimeOffset? EnqueueTime { get; } = enqueueTime;

        public bool IsReserved(IMessageSink<FabricMessage> sink)
        {
            lock (_delivered)
                return _delivered.Contains(sink);
        }

        public bool TryReserveDelivery(IMessageSink<FabricMessage> sink)
        {
            ArgumentNullException.ThrowIfNull(sink);
            lock (_delivered)
                return _delivered.Add(sink);
        }
    }

    private sealed class RecordingSink : IMessageSink<FabricMessage>
    {
        private readonly ConcurrentQueue<string> _values = new();

        public string[] Values => _values.ToArray();

        public Task DeliverAsync(IMessageDeliveryContext<FabricMessage> context, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _values.Enqueue(context.Message.Value);
            return Task.CompletedTask;
        }

        public void Probe(ProbeContext context)
        {
        }
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
            cancellationToken.ThrowIfCancellationRequested();
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

    private sealed class FailOnceReceiver : IMessageReceiver<FabricMessage>
    {
        private readonly TaskCompletionSource _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly ConcurrentQueue<string> _values = new();
        private int _attempts;

        public int Attempts => Volatile.Read(ref _attempts);
        public Task Completed => _completed.Task;
        public string[] Values => _values.ToArray();

        public Task DeliverAsync(FabricMessage message, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Interlocked.Increment(ref _attempts) == 1)
                throw new InvalidOperationException("Expected receiver failure.");

            _values.Enqueue(message.Value);
            _completed.TrySetResult();
            return Task.CompletedTask;
        }

        public void Probe(ProbeContext context)
        {
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
}
