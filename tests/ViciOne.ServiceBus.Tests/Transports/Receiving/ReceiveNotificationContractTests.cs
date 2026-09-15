using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Observers;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Mediator.Contexts;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transports.Receiving;

public sealed class ReceiveNotificationContractTests
{
    static readonly TimeSpan Duration = TimeSpan.FromTicks(87);
    const string ConsumerType = "notification-consumer";
    const string MessageType = "ViciOne.ServiceBus.Tests.Transports.Receiving.ReceiveNotificationContractTests+NotificationMessage";

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-RECEIVE-NOTIFICATION", "arguments-before-cancellation-without-side-effects")]
    public void InvalidArguments_AreRejectedBeforeCancellationWithoutChangingState(bool useMediator, bool cancel)
    {
        using var fixture = new Fixture(useMediator);
        using var cancellation = new CancellationTokenSource();
        if (cancel)
            cancellation.Cancel();
        CancellationToken token = cancellation.Token;
        ReceiveContext receive = fixture.Receive;
        ConsumeContext<NotificationMessage> consume = fixture.Consume;
        var failure = new InvalidOperationException("consumer failure");

        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = receive.NotifyConsumedAsync<NotificationMessage>(null!, Duration, ConsumerType, token);
        }).ParamName);
        Assert.Equal("consumerType", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = receive.NotifyConsumedAsync(consume, Duration, null!, token);
        }).ParamName);
        foreach (string invalid in new[] { "", " ", "\t" })
        {
            Assert.Equal("consumerType", Assert.Throws<ArgumentException>(() =>
            {
                _ = receive.NotifyConsumedAsync(consume, Duration, invalid, token);
            }).ParamName);
            Assert.Equal("consumerType", Assert.Throws<ArgumentException>(() =>
            {
                _ = receive.NotifyFaultedAsync(consume, Duration, invalid, failure, token);
            }).ParamName);
        }

        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = receive.NotifyFaultedAsync<NotificationMessage>(null!, Duration, null!, null!, token);
        }).ParamName);
        Assert.Equal("consumerType", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = receive.NotifyFaultedAsync(consume, Duration, null!, null!, token);
        }).ParamName);
        Assert.Equal("exception", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = receive.NotifyFaultedAsync(consume, Duration, ConsumerType, null!, token);
        }).ParamName);
        Assert.Equal("exception", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = receive.NotifyFaultedAsync(null!, token);
        }).ParamName);

        AssertNoSideEffects(fixture);
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 0)]
    [InlineData(true, 1)]
    [InlineData(true, 2)]
    [RequirementCoverage("REQ-VSB-RECEIVE-NOTIFICATION", "caller-cancellation-preserves-token-and-state")]
    public async Task CanceledNotification_PreservesTheCallerTokenAndHasNoSideEffectsAsync(bool useMediator, int operation)
    {
        using var fixture = new Fixture(useMediator);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Task notification = NotifyAsync(fixture, operation, new InvalidOperationException("consumer failure"), cancellation.Token);
        Assert.True(notification.IsCanceled);
        OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => notification);
        Assert.Equal(cancellation.Token, canceled.CancellationToken);
        AssertNoSideEffects(fixture);
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 0)]
    [InlineData(true, 1)]
    [InlineData(true, 2)]
    [RequirementCoverage("REQ-VSB-RECEIVE-NOTIFICATION", "held-observer-task-and-exact-arguments")]
    public async Task HeldObserverTask_IsForwardedWithExactArgumentsAndDeliveryStateAsync(bool useMediator, int operation)
    {
        using var fixture = new Fixture(useMediator);
        using var cancellation = new CancellationTokenSource();
        var held = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Observer.ReturnedTask = held.Task;
        var failure = new InvalidOperationException("consumer failure");
        Task? notification = null;

        try
        {
            notification = NotifyAsync(fixture, operation, failure, cancellation.Token);
            Assert.Same(held.Task, notification);
            Assert.False(notification.IsCompleted);
            cancellation.Cancel();
            Assert.False(notification.IsCompleted);
            AssertRecordedNotification(fixture, operation, failure);
        }
        finally
        {
            held.TrySetResult();
            if (notification is not null)
                await notification.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
        }

        Assert.NotNull(notification);
        Assert.True(notification.IsCompletedSuccessfully);
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 0)]
    [InlineData(true, 1)]
    [InlineData(true, 2)]
    [RequirementCoverage("REQ-VSB-RECEIVE-NOTIFICATION", "null-observer-task-has-operation-specific-diagnostic")]
    public void NullObserverTask_IsRejectedWithAnOperationSpecificDiagnostic(bool useMediator, int operation)
    {
        using var fixture = new Fixture(useMediator);
        fixture.Observer.ReturnedTask = null;
        var failure = new InvalidOperationException("consumer failure");

        InvalidOperationException rejected = Assert.Throws<InvalidOperationException>(() =>
        {
            _ = NotifyAsync(fixture, operation, failure, TestContext.Current.CancellationToken);
        });
        string expected = operation switch
        {
            0 => "The receive observer returned no post-consume task.",
            1 => "The receive observer returned no consume-fault task.",
            2 => "The receive observer returned no receive-fault task.",
            _ => throw new ArgumentOutOfRangeException(nameof(operation)),
        };
        Assert.Equal(expected, rejected.Message);
        AssertRecordedNotification(fixture, operation, failure);
    }

    [Theory]
    [InlineData(false, 0, false)]
    [InlineData(false, 1, false)]
    [InlineData(false, 2, false)]
    [InlineData(true, 0, false)]
    [InlineData(true, 1, false)]
    [InlineData(true, 2, false)]
    [InlineData(false, 0, true)]
    [InlineData(false, 1, true)]
    [InlineData(false, 2, true)]
    [InlineData(true, 0, true)]
    [InlineData(true, 1, true)]
    [InlineData(true, 2, true)]
    [RequirementCoverage("REQ-VSB-RECEIVE-NOTIFICATION", "observer-task-fault-and-cancellation-remain-original")]
    public async Task FaultedOrCanceledObserverTask_PreservesTheOriginalOutcomeAsync(bool useMediator, int operation, bool cancelObserver)
    {
        using var fixture = new Fixture(useMediator);
        using var observerCancellation = new CancellationTokenSource();
        observerCancellation.Cancel();
        var observerFailure = new InvalidOperationException("observer failure");
        fixture.Observer.ReturnedTask = cancelObserver
            ? Task.FromCanceled(observerCancellation.Token)
            : Task.FromException(observerFailure);
        var consumerFailure = new InvalidOperationException("consumer failure");

        Task notification = NotifyAsync(fixture, operation, consumerFailure, TestContext.Current.CancellationToken);
        Assert.Same(fixture.Observer.ReturnedTask, notification);
        if (cancelObserver)
        {
            OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => notification);
            Assert.True(notification.IsCanceled);
            Assert.Equal(observerCancellation.Token, canceled.CancellationToken);
        }
        else
        {
            InvalidOperationException faulted = await Assert.ThrowsAsync<InvalidOperationException>(() => notification);
            Assert.True(notification.IsFaulted);
            Assert.Same(observerFailure, faulted);
        }

        AssertRecordedNotification(fixture, operation, consumerFailure);
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 0)]
    [InlineData(true, 1)]
    [InlineData(true, 2)]
    [RequirementCoverage("REQ-VSB-RECEIVE-NOTIFICATION", "synchronous-observer-fault-remains-original")]
    public void ThrowingObserver_PropagatesTheOriginalFailureAfterRecordingState(bool useMediator, int operation)
    {
        using var fixture = new Fixture(useMediator);
        var observerFailure = new InvalidOperationException("synchronous observer failure");
        fixture.Observer.ThrownException = observerFailure;
        var consumerFailure = new InvalidOperationException("consumer failure");

        InvalidOperationException faulted = Assert.Throws<InvalidOperationException>(() =>
        {
            _ = NotifyAsync(fixture, operation, consumerFailure, TestContext.Current.CancellationToken);
        });
        Assert.Same(observerFailure, faulted);
        AssertRecordedNotification(fixture, operation, consumerFailure);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-RECEIVE-NOTIFICATION", "existing-consumer-fault-metadata-is-preserved")]
    public async Task ConsumerFaultNotification_PreservesExistingFaultMetadataAsync(bool useMediator)
    {
        using var fixture = new Fixture(useMediator);
        var existing = new SeedFaultContext("original-message", "original-consumer");
        fixture.Receive.GetOrAddPayload<ConsumerFaultContext>(() => existing);
        fixture.ExistingFaultContext = existing;
        var failure = new InvalidOperationException("consumer failure");

        await NotifyAsync(fixture, 1, failure, TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        Assert.True(fixture.Receive.TryGetPayload(out ConsumerFaultContext? retained));
        Assert.Same(existing, retained);
        Assert.Equal("original-message", retained.MessageType);
        Assert.Equal("original-consumer", retained.ConsumerType);
        AssertRecordedNotification(fixture, 1, failure);
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 0)]
    [InlineData(true, 1)]
    [InlineData(true, 2)]
    [RequirementCoverage("REQ-VSB-RECEIVE-NOTIFICATION", "connected-null-observer-remains-a-faulted-task")]
    public async Task ConnectedNullObserver_ReturnsFaultedTaskWithoutLosingDeliveryStateAsync(bool useMediator, int operation)
    {
        var observer = new RecordingObserver { ReturnedTask = null };
        var connected = new ReceiveObservable();
        using ConnectHandle registration = connected.Connect(observer);
        using var fixture = new Fixture(useMediator, observer, connected);
        var failure = new InvalidOperationException("consumer failure");

        Task notification = NotifyAsync(fixture, operation, failure, TestContext.Current.CancellationToken);
        Assert.NotNull(notification);
        InvalidOperationException rejected = await Assert.ThrowsAsync<InvalidOperationException>(() => notification);
        Assert.True(notification.IsFaulted);
        Assert.Equal("The connection callback returned a null task.", rejected.Message);
        AssertRecordedNotification(fixture, operation, failure);
    }

    static Task NotifyAsync(Fixture fixture, int operation, Exception failure, CancellationToken cancellationToken)
    {
        return operation switch
        {
            0 => fixture.Receive.NotifyConsumedAsync(fixture.Consume, Duration, ConsumerType, cancellationToken),
            1 => fixture.Receive.NotifyFaultedAsync(fixture.Consume, Duration, ConsumerType, failure, cancellationToken),
            2 => fixture.Receive.NotifyFaultedAsync(failure, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(operation)),
        };
    }

    static void AssertNoSideEffects(Fixture fixture)
    {
        Assert.False(fixture.Receive.IsDelivered);
        Assert.False(fixture.Receive.IsFaulted);
        Assert.Empty(fixture.Observer.Calls);
        Assert.False(fixture.Receive.TryGetPayload(out ConsumerFaultContext? _));
    }

    static void AssertRecordedNotification(Fixture fixture, int operation, Exception failure)
    {
        Assert.Equal(operation == 0, fixture.Receive.IsDelivered);
        Assert.Equal(operation != 0, fixture.Receive.IsFaulted);
        Notification call = Assert.Single(fixture.Observer.Calls);
        Assert.Equal(operation == 0, call.IsDelivered);
        Assert.Equal(operation != 0, call.IsFaulted);
        Assert.Equal(operation, call.Operation);
        Assert.Same(operation == 2 ? fixture.Receive : fixture.Consume, call.Context);
        Assert.Equal(operation == 2 ? TimeSpan.Zero : Duration, call.Duration);
        Assert.Equal(operation == 2 ? null : ConsumerType, call.ConsumerType);
        Assert.Same(operation == 0 ? null : failure, call.Exception);
        Assert.Equal(operation == 1, fixture.Receive.TryGetPayload(out ConsumerFaultContext? fault));
        if (operation == 1)
        {
            Assert.NotNull(fault);
            Assert.Same(fault, call.FaultContext);
            if (fixture.ExistingFaultContext is { } existing)
                Assert.Same(existing, fault);
            string expectedMessageType = fixture.ExistingFaultContext?.MessageType ?? MessageType;
            string expectedConsumerType = fixture.ExistingFaultContext?.ConsumerType ?? ConsumerType;
            Assert.Equal(expectedMessageType, fault.MessageType);
            Assert.Equal(expectedConsumerType, fault.ConsumerType);
            Assert.Equal(expectedMessageType, call.MessageType);
            Assert.Equal(expectedConsumerType, call.FaultConsumerType);
        }
        else
        {
            Assert.Null(call.FaultContext);
            Assert.Null(call.MessageType);
            Assert.Null(call.FaultConsumerType);
        }
    }

    sealed class Fixture : IDisposable
    {
        public Fixture(bool useMediator, RecordingObserver? observer = null, IReceiveObserver? notifications = null)
        {
            Observer = observer ?? new RecordingObserver();
            notifications ??= Observer;
            var message = new NotificationMessage("notification");
            var send = new MessageSendContext<NotificationMessage>(message, TestContext.Current.CancellationToken)
            {
                DestinationAddress = new Uri("loopback://localhost/notification"),
            };
            var body = new BinaryMessageBody("{\"value\":\"notification\"}"u8.ToArray());
            IPublishEndpointProvider publishEndpoints = StrictProxy.CreateUnused<IPublishEndpointProvider>();

            if (useMediator)
            {
                Receive = new MediatorReceiveContext<NotificationMessage>(send,
                    StrictProxy.CreateUnused<ISendEndpointProvider>(),
                    publishEndpoints,
                    StrictProxy.CreateUnused<IPublishTopology>(),
                    notifications, ServiceBusMetadataJson.ObjectDeserializer, body);
            }
            else
            {
                ReceiveEndpointContext endpoint = StrictProxy.Create<ReceiveEndpointContext>((method, args) =>
                {
                    if (method.Name == "get_InputAddress")
                        return send.DestinationAddress;
                    if (method.Name == "get_ReceiveObservers")
                        return notifications;
                    if (method.Name == "get_PublishEndpointProvider")
                        return publishEndpoints;
                    if (method.Name == nameof(PipeContext.TryGetPayload))
                        return method.Invoke(send, args);
                    throw new InvalidOperationException($"Unexpected receive endpoint operation: {method.Name}.");
                });
                Receive = new TransportContext(endpoint, body);
            }

            var metadata = new MediatorSendMessageContext<NotificationMessage>(send);
            var serialization = new MediatorSerializationContext<NotificationMessage>(
                ServiceBusMetadataJson.ObjectDeserializer, metadata, message, send.SupportedMessageTypes);
            Consume = new MediatorConsumeContext<NotificationMessage>(Receive, serialization, message);
        }

        public ReceiveContext Receive { get; }
        public ConsumeContext<NotificationMessage> Consume { get; }
        public RecordingObserver Observer { get; }
        public ConsumerFaultContext? ExistingFaultContext { get; set; }

        public void Dispose()
        {
            if (Receive is IDisposable disposable)
                disposable.Dispose();
        }
    }

    sealed class TransportContext(ReceiveEndpointContext endpoint, MessageBody body) : BaseReceiveContext(false, endpoint)
    {
        protected override IHeaderProvider HeaderProvider => new DictionarySendHeaderProvider(new DictionarySendHeaders());
        public override MessageBody Body => body;
    }

    class StrictProxy : DispatchProxy
    {
        Func<MethodInfo, object?[]?, object?>? _dispatch;

        public static T Create<T>(Func<MethodInfo, object?[]?, object?> dispatch)
            where T : class
        {
            T proxy = DispatchProxy.Create<T, StrictProxy>();
            ((StrictProxy)(object)proxy)._dispatch = dispatch;
            return proxy;
        }

        public static T CreateUnused<T>()
            where T : class
        {
            return Create<T>((method, _) => throw new InvalidOperationException($"Unexpected dependency operation: {method.Name}."));
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            return (_dispatch ?? throw new InvalidOperationException("The dependency dispatch is not configured."))(targetMethod, args);
        }
    }

    sealed class RecordingObserver : IReceiveObserver
    {
        public Task? ReturnedTask { get; set; } = Task.CompletedTask;
        public Exception? ThrownException { get; set; }
        public List<Notification> Calls { get; } = [];

        public Task PreReceiveAsync(ReceiveContext context) => throw new InvalidOperationException("Unexpected pre-receive notification.");
        public Task PostReceiveAsync(ReceiveContext context) => throw new InvalidOperationException("Unexpected post-receive notification.");

        public Task PostConsumeAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType)
            where T : class
        {
            RecordNotification(0, context, context.Advanced().ReceiveContext, duration, consumerType, null);
            return GetCompletionAsync();
        }

        public Task ConsumeFaultAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception)
            where T : class
        {
            RecordNotification(1, context, context.Advanced().ReceiveContext, duration, consumerType, exception);
            return GetCompletionAsync();
        }

        public Task ReceiveFaultAsync(ReceiveContext context, Exception exception)
        {
            RecordNotification(2, context, context, TimeSpan.Zero, null, exception);
            return GetCompletionAsync();
        }

        void RecordNotification(int operation, object context, ReceiveContext receive, TimeSpan duration, string? consumerType, Exception? exception)
        {
            receive.TryGetPayload(out ConsumerFaultContext? fault);
            Calls.Add(new Notification(operation, context, duration, consumerType, exception,
                receive.IsDelivered, receive.IsFaulted, fault, fault?.MessageType, fault?.ConsumerType));
        }

        Task GetCompletionAsync()
        {
            if (ThrownException is { } exception)
                throw exception;
            return ReturnedTask!;
        }
    }

    sealed record Notification(int Operation, object Context, TimeSpan Duration, string? ConsumerType, Exception? Exception,
        bool IsDelivered, bool IsFaulted, ConsumerFaultContext? FaultContext, string? MessageType, string? FaultConsumerType);
    sealed record SeedFaultContext(string MessageType, string ConsumerType) : ConsumerFaultContext;
    sealed record NotificationMessage(string Value);
}
