using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using ViciOne.ServiceBus.RabbitMq.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.RabbitMqTransport;

public sealed class RabbitMqConsumerFilterTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CONSUMER-LIFECYCLE", "delivery-metadata-and-ack-mode")]
    public async Task HandleBasicDeliverAsync_ForwardsBrokerDeliveryWithTheConfiguredAckModeAsync(bool noAck)
    {
        var harness = new ConsumerHarness(noAck: noAck);
        Task send = harness.Filter.SendAsync(harness.ChannelContext, harness.Next);
        var properties = new BasicProperties { MessageId = "order-17" };
        byte[] body = [1, 2, 3, 4];

        await harness.Consumer.HandleBasicDeliverAsync("assigned-tag", 17, true, "orders", "orders.created",
            properties, body, TestContext.Current.CancellationToken);

        RabbitMqReceiveContext received = Assert.IsType<RabbitMqReceiveContext>(harness.DispatchedContext);
        Assert.Equal("orders", received.Exchange);
        Assert.Equal("orders.created", received.RoutingKey);
        Assert.Equal("assigned-tag", received.ConsumerTag);
        Assert.Equal(17UL, received.DeliveryTag);
        Assert.True(received.Redelivered);
        Assert.Same(properties, received.Properties);
        Assert.Equal(body, harness.DispatchedBody);
        Assert.Equal(noAck ? 0 : 1, harness.AckCalls);
        if (!noAck)
            Assert.Equal(17UL, harness.AckTag);

        await harness.Consumer.HandleBasicCancelOkAsync("assigned-tag", TestContext.Current.CancellationToken);
        await send.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal(1, harness.DispatchCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CONSUMER-LIFECYCLE", "pre-canceled-delivery-skips-dispatch")]
    public async Task HandleBasicDeliverAsync_PreCanceledCallbackDoesNotDispatchAsync()
    {
        var harness = new ConsumerHarness();
        Task send = harness.Filter.SendAsync(harness.ChannelContext, harness.Next);
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();

        OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            harness.Consumer.HandleBasicDeliverAsync("assigned-tag", 18, false, "orders", "orders.created",
                new BasicProperties(), new byte[] { 1 }, canceled.Token));

        Assert.Equal(canceled.Token, actual.CancellationToken);
        Assert.Equal(0, harness.DispatchCalls);
        await harness.Consumer.HandleBasicCancelOkAsync("assigned-tag", TestContext.Current.CancellationToken);
        await send.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CONSUMER-LIFECYCLE", "broker-dispatch-fault-invalidates-consumer")]
    public async Task HandleBasicDeliverAsync_BrokerDispatchFailureFaultsChannelAndCompletesConsumerAsync(bool streamEnded)
    {
        var harness = new ConsumerHarness();
        Exception failure = streamEnded
            ? new EndOfStreamException("broker stream ended")
            : new OperationInterruptedException(new ShutdownEventArgs(ShutdownInitiator.Peer, 404, "queue removed"));
        harness.DispatchFailure = failure;
        Task send = harness.Filter.SendAsync(harness.ChannelContext, harness.Next);

        await harness.Consumer.HandleBasicDeliverAsync("assigned-tag", 19, false, "orders", "orders.created",
            new BasicProperties(), new byte[] { 9 }, TestContext.Current.CancellationToken);
        await send.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.Equal(1, harness.DispatchCalls);
        Assert.Same(failure, harness.NotifiedFault);
        Assert.Equal(harness.InputAddress, harness.FaultedAddress);
        Assert.Equal(0, harness.AckCalls);
        Assert.Equal(1, harness.NextCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CONSUMER-LIFECYCLE", "ordinary-dispatch-fault-keeps-consumer-active")]
    public async Task HandleBasicDeliverAsync_OrdinaryDispatchFailureDoesNotFaultTheChannelAsync()
    {
        var harness = new ConsumerHarness();
        harness.DispatchFailure = new InvalidOperationException("handler failed");
        Task send = harness.Filter.SendAsync(harness.ChannelContext, harness.Next);

        await harness.Consumer.HandleBasicDeliverAsync("assigned-tag", 20, false, "orders", "orders.created",
            new BasicProperties(), new byte[] { 1 }, TestContext.Current.CancellationToken);

        Assert.Null(harness.NotifiedFault);
        Assert.Equal(0, harness.AckCalls);
        Assert.False(send.IsCompleted);

        harness.DispatchFailure = null;
        await harness.Consumer.HandleBasicDeliverAsync("assigned-tag", 21, false, "orders", "orders.created",
            new BasicProperties(), new byte[] { 2 }, TestContext.Current.CancellationToken);

        Assert.Equal(2, harness.DispatchCalls);
        Assert.Equal(1, harness.AckCalls);
        Assert.Equal(21UL, harness.AckTag);
        await harness.Consumer.HandleBasicCancelOkAsync("assigned-tag", TestContext.Current.CancellationToken);
        await send.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CONSUMER-LIFECYCLE", "late-delivery-after-stop-is-ignored")]
    public async Task HandleBasicDeliverAsync_DoesNotDispatchAfterConsumerStoppedAsync()
    {
        var harness = new ConsumerHarness();
        Task send = harness.Filter.SendAsync(harness.ChannelContext, harness.Next);
        await harness.Consumer.HandleBasicCancelOkAsync("assigned-tag", TestContext.Current.CancellationToken);
        await send.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        await harness.Consumer.HandleBasicDeliverAsync("assigned-tag", 22, false, "orders", "orders.created",
            new BasicProperties(), new byte[] { 3 }, TestContext.Current.CancellationToken);

        Assert.Equal(0, harness.DispatchCalls);
        Assert.Equal(0, harness.AckCalls);
        Assert.Null(harness.NotifiedFault);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CONSUMER-LIFECYCLE", "broker-start-ready-complete-before-next")]
    public async Task SendAsync_UsesBrokerSettingsAndWaitsForConsumerCompletionBeforeContinuingAsync()
    {
        var harness = new ConsumerHarness(autoConfirmConsumer: false);

        Task send = harness.Filter.SendAsync(harness.ChannelContext, harness.Next);

        Assert.False(send.IsCompleted);
        Assert.Equal(["consume"], harness.Events.ToArray());
        Assert.Null(harness.Agent);
        Assert.Null(harness.ReadyAddress);
        Assert.Equal(0, harness.NextCalls);

        await harness.Consumer.HandleBasicConsumeOkAsync("assigned-tag", CancellationToken.None);
        await harness.ReadyObserved.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.Equal(["consume", "agent", "ready"], harness.Events.ToArray());
        Assert.Equal("orders.queue", harness.Queue);
        Assert.False(harness.NoAck);
        Assert.True(harness.Exclusive);
        Assert.Same(harness.ConsumeArguments, harness.Arguments);
        Assert.Equal("requested-tag", harness.RequestedTag);
        Assert.Equal(harness.CancellationToken, harness.ConsumeToken);
        Assert.NotNull(harness.Consumer);
        Assert.Same(harness.Consumer, harness.Agent);
        Assert.Equal(harness.InputAddress, harness.ReadyAddress);
        Assert.True(harness.ReadyIsStarted);
        Assert.Equal(0, harness.NextCalls);

        await harness.Consumer.HandleBasicCancelOkAsync("assigned-tag", CancellationToken.None);
        await send.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.Equal(["consume", "agent", "ready", "completed", "next"], harness.Events.ToArray());
        Assert.Equal(harness.InputAddress, harness.CompletedAddress);
        Assert.Equal(0, harness.DeliveryCount);
        Assert.Equal(0, harness.MaxConcurrentDeliveryCount);
        Assert.Equal(1, harness.NextCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CONSUMER-LIFECYCLE", "broker-start-cancellation-preserves-cause")]
    public async Task SendAsync_PropagatesBrokerStartCancellationWithoutPublishingReadinessAsync()
    {
        var harness = new ConsumerHarness();
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        var failure = new OperationCanceledException("consumer start canceled", canceled.Token);
        harness.ConsumeFailure = failure;

        OperationCanceledException observed = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            harness.Filter.SendAsync(harness.ChannelContext, harness.Next));

        Assert.Same(failure, observed);
        Assert.Equal(["consume"], harness.Events.ToArray());
        Assert.Null(harness.Agent);
        Assert.Null(harness.ReadyAddress);
        Assert.Null(harness.CompletedAddress);
        Assert.Equal(0, harness.NextCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CONSUMER-LIFECYCLE", "broker-assigned-tag-survives-restart")]
    public async Task SendAsync_ReusesTheBrokerAssignedConsumerTagAfterAChannelRestartAsync()
    {
        var harness = new ConsumerHarness(requestedTag: "");

        Task first = harness.Filter.SendAsync(harness.ChannelContext, harness.Next);
        Assert.Equal("", harness.RequestedTag);
        await harness.Consumer.HandleBasicCancelOkAsync("assigned-tag", CancellationToken.None);
        await first.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Task second = harness.Filter.SendAsync(harness.ChannelContext, harness.Next);
        Assert.Equal("assigned-tag", harness.RequestedTag);
        Assert.False(second.IsCompleted);
        await harness.Consumer.HandleBasicCancelOkAsync("assigned-tag", CancellationToken.None);
        await second.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.Equal(2, harness.NextCalls);
        Assert.Equal(["consume", "agent", "ready", "completed", "next",
            "consume", "agent", "ready", "completed", "next"], harness.Events.ToArray());
    }

    private sealed class ConsumerHarness
    {
        public readonly Uri InputAddress = new("rabbitmq://broker/production/orders.queue");
        public readonly IDictionary<string, object?> ConsumeArguments = new Dictionary<string, object?> { ["x-priority"] = 7 };
        public readonly ConcurrentQueue<string> Events = new();
        public readonly TaskCompletionSource<bool> ReadyObserved = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly CancellationToken CancellationToken = new CancellationTokenSource().Token;

        public ConsumerHarness(string requestedTag = "requested-tag", bool autoConfirmConsumer = true, bool noAck = false)
        {
            _autoConfirmConsumer = autoConfirmConsumer;
            ReceiveSettings settings = Proxy<ReceiveSettings>((method, _) => method.Name switch
            {
                "get_QueueName" => "orders.queue",
                "get_NoAck" => noAck,
                "get_ConsumerTag" => requestedTag,
                "get_ConsumeArguments" => ConsumeArguments,
                _ => throw Unexpected(method),
            });
            IChannel channel = Proxy<IChannel>((method, _) => method.Name switch
            {
                "add_ChannelShutdownAsync" or "remove_ChannelShutdownAsync" => null,
                "get_IsOpen" => true,
                "get_IsClosed" => false,
                _ => throw Unexpected(method),
            });
            IReceivePipeDispatcher dispatcher = Proxy<IReceivePipeDispatcher>((method, _) => method.Name switch
            {
                "add_ZeroActivity" or "remove_ZeroActivity" => null,
                "get_ActiveDispatchCount" or "get_MaxConcurrentDispatchCount" => 0,
                "get_DispatchCount" => 0L,
                "DispatchAsync" => RecordDispatchAsync(_!),
                _ => throw Unexpected(method),
            });
            IReceiveTransportObserver observers = Proxy<IReceiveTransportObserver>((method, args) => method.Name switch
            {
                "ReadyAsync" => RecordReadyAsync((ReceiveTransportReady)args![0]!),
                "CompletedAsync" => RecordCompletedAsync((ReceiveTransportCompleted)args![0]!),
                _ => throw Unexpected(method),
            });
            RabbitMqReceiveEndpointContext endpoint = Proxy<RabbitMqReceiveEndpointContext>((method, args) => method.Name switch
            {
                "CreateReceivePipeDispatcher" => dispatcher,
                "TryGetPayload" => NoPayload(args!),
                "get_TransportObservers" => observers,
                "get_InputAddress" => InputAddress,
                "get_ExclusiveConsumer" => true,
                "get_LogContext" => null,
                "get_StopTimeout" or "get_ConsumerStopTimeout" => null,
                "AddConsumeAgent" => RecordAgent((IAgent)args![0]!),
                _ => throw Unexpected(method),
            });
            ConnectionContext connection = Proxy<ConnectionContext>((_, _) =>
                throw new InvalidOperationException("Connection data was requested."));
            ChannelContext = Proxy<ChannelContext>((method, args) => method.Name switch
            {
                "TryGetPayload" => SupplySettings(args!, settings),
                "get_Channel" => channel,
                "get_ConnectionContext" => connection,
                "get_CancellationToken" => CancellationToken,
                "BasicAckAsync" => RecordAckAsync(args!),
                "NotifyFaulted" => RecordFault(args!),
                "BasicConsumeAsync" => ConsumeAsync(args!),
                _ => throw Unexpected(method),
            });
            Next = Proxy<IPipe<ChannelContext>>((method, args) => method.Name switch
            {
                "SendAsync" => ContinueAsync((ChannelContext)args![0]!),
                "Probe" => null,
                _ => throw Unexpected(method),
            });
            Filter = new RabbitMqConsumerFilter(endpoint);
        }

        public RabbitMqConsumerFilter Filter { get; }
        public ChannelContext ChannelContext { get; }
        public IPipe<ChannelContext> Next { get; }
        public Exception? ConsumeFailure { get; set; }
        public RabbitMqBasicConsumer Consumer { get; private set; } = null!;
        public IAgent? Agent { get; private set; }
        public string? Queue { get; private set; }
        public bool NoAck { get; private set; }
        public bool Exclusive { get; private set; }
        public IDictionary<string, object?>? Arguments { get; private set; }
        public string? RequestedTag { get; private set; }
        public CancellationToken ConsumeToken { get; private set; }
        public Uri? ReadyAddress { get; private set; }
        public bool ReadyIsStarted { get; private set; }
        public Uri? CompletedAddress { get; private set; }
        public long DeliveryCount { get; private set; }
        public int MaxConcurrentDeliveryCount { get; private set; }
        public int NextCalls { get; private set; }
        public Exception? DispatchFailure { get; set; }
        public Exception? NotifiedFault { get; private set; }
        public Uri? FaultedAddress { get; private set; }
        public int DispatchCalls { get; private set; }
        public ReceiveContext? DispatchedContext { get; private set; }
        public ReceiveLockContext? DispatchedLock { get; private set; }
        public byte[]? DispatchedBody { get; private set; }
        public int AckCalls { get; private set; }
        public ulong AckTag { get; private set; }
        private readonly bool _autoConfirmConsumer;

        private Task<string> ConsumeAsync(object?[] args)
        {
            Events.Enqueue("consume");
            Queue = (string)args[0]!;
            NoAck = (bool)args[1]!;
            Exclusive = (bool)args[2]!;
            Arguments = (IDictionary<string, object?>)args[3]!;
            Consumer = Assert.IsType<RabbitMqBasicConsumer>(args[4]);
            RequestedTag = (string)args[5]!;
            ConsumeToken = (CancellationToken)args[6]!;

            if (ConsumeFailure is { } failure)
                return Task.FromException<string>(failure);

            if (_autoConfirmConsumer)
            {
                Task ready = Consumer.HandleBasicConsumeOkAsync("assigned-tag", CancellationToken.None);
                Assert.True(ready.IsCompletedSuccessfully);
            }
            return Task.FromResult("assigned-tag");
        }

        private object? RecordAgent(IAgent agent)
        {
            Events.Enqueue("agent");
            Agent = agent;
            return null;
        }

        private Task RecordReadyAsync(ReceiveTransportReady ready)
        {
            Events.Enqueue("ready");
            ReadyAddress = ready.InputAddress;
            ReadyIsStarted = ready.IsStarted;
            ReadyObserved.TrySetResult(true);
            return Task.CompletedTask;
        }

        private Task RecordCompletedAsync(ReceiveTransportCompleted completed)
        {
            Events.Enqueue("completed");
            CompletedAddress = completed.InputAddress;
            DeliveryCount = completed.DeliveryCount;
            MaxConcurrentDeliveryCount = completed.MaxConcurrentDeliveryCount;
            return Task.CompletedTask;
        }

        private Task ContinueAsync(ChannelContext context)
        {
            Assert.Same(ChannelContext, context);
            Events.Enqueue("next");
            NextCalls++;
            return Task.CompletedTask;
        }

        private async Task RecordDispatchAsync(object?[]? args)
        {
            DispatchCalls++;
            DispatchedContext = Assert.IsAssignableFrom<ReceiveContext>(args![0]);
            DispatchedLock = Assert.IsAssignableFrom<ReceiveLockContext>(args[1]);
            DispatchedBody = DispatchedContext.Body.ToArray();
            if (DispatchFailure is { } failure)
                throw failure;
            await DispatchedLock.CompleteAsync(TestContext.Current.CancellationToken);
        }

        private object? RecordFault(object?[] args)
        {
            NotifiedFault = Assert.IsAssignableFrom<Exception>(args[0]);
            FaultedAddress = Assert.IsType<Uri>(args[1]);
            return null;
        }

        private ValueTask RecordAckAsync(object?[] args)
        {
            AckCalls++;
            AckTag = Assert.IsType<ulong>(args[0]);
            Assert.False(Assert.IsType<bool>(args[1]));
            return ValueTask.CompletedTask;
        }

        private static bool SupplySettings(object?[] args, ReceiveSettings settings)
        {
            args[0] = settings;
            return true;
        }

        private static bool NoPayload(object?[] args)
        {
            args[0] = null;
            return false;
        }

        private static Exception Unexpected(MethodInfo method) => new NotSupportedException($"Unexpected call: {method.Name}");

        private static T Proxy<T>(Func<MethodInfo, object?[]?, object?> handler) where T : class
        {
            T result = DispatchProxy.Create<T, StrictProxy<T>>();
            ((StrictProxy<T>)(object)result).Handler = handler;
            return result;
        }
    }

    private class StrictProxy<T> : DispatchProxy where T : class
    {
        public Func<MethodInfo, object?[]?, object?> Handler { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            Handler(targetMethod ?? throw new InvalidOperationException("No proxy method was supplied."), args);
    }
}
