using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Mediator.Contexts;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Context.Consumption;

public sealed class ConsumeNotificationContractTests
{
    static readonly TimeSpan Duration = TimeSpan.FromTicks(113);
    const string ConsumerType = "consume-notification-consumer";

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-CONSUME-NOTIFICATION", "synchronous-arguments-before-cancellation")]
    public void InvalidArguments_AreRejectedSynchronouslyBeforeCancellation(bool faulted, bool cancel)
    {
        using var fixture = new Fixture();
        using var caller = new CancellationTokenSource();
        if (cancel)
            caller.Cancel();
        var failure = new InvalidOperationException("consumer failure");

        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = NotifyAsync(fixture.Owner, null!, faulted, null!, null!, caller.Token);
        }).ParamName);
        Assert.Equal("consumerType", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = NotifyAsync(fixture.Owner, fixture.MessageContext, faulted, null!, null!, caller.Token);
        }).ParamName);
        foreach (string invalid in new[] { "", " ", "\t" })
        {
            Assert.Equal("consumerType", Assert.Throws<ArgumentException>(() =>
            {
                _ = NotifyAsync(fixture.Owner, fixture.MessageContext, faulted, invalid, failure, caller.Token);
            }).ParamName);
        }
        if (faulted)
        {
            Assert.Equal("exception", Assert.Throws<ArgumentNullException>(() =>
            {
                _ = NotifyAsync(fixture.Owner, fixture.MessageContext, true, ConsumerType, null!, caller.Token);
            }).ParamName);
        }

        Assert.Empty(fixture.Owner.GeneratedFaults);
        AssertNoReceiveEffects(fixture);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-CONSUME-NOTIFICATION", "pre-cancellation-has-no-effects-and-keeps-caller-token")]
    public async Task PreCanceledNotification_HasNoEffectsAndPreservesTheCallerTokenAsync(bool faulted, bool useBoundary)
    {
        using var fixture = new Fixture();
        using var caller = new CancellationTokenSource();
        caller.Cancel();
        var boundary = new ReceiveBoundary(fixture);
        RecordingContext owner = useBoundary ? boundary.Owner : fixture.Owner;

        Task notification = NotifyAsync(owner, fixture.MessageContext, faulted, ConsumerType,
            new InvalidOperationException("consumer failure"), caller.Token);
        Assert.True(notification.IsCanceled);
        OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => notification);
        Assert.Equal(caller.Token, canceled.CancellationToken);
        Assert.Empty(owner.GeneratedFaults);
        Assert.Empty(boundary.Calls);
        AssertNoReceiveEffects(fixture);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-CONSUME-NOTIFICATION", "held-generation-is-owned-before-receive-and-caller-cancellation")]
    public async Task HeldGeneration_IsOwnedAndCallerCancellationDoesNotDetachItAsync(bool cancel)
    {
        using var fixture = new Fixture();
        using var caller = new CancellationTokenSource();
        var generation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var receive = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Owner.GenerationTask = generation.Task;
        fixture.Observer.ReturnedTask = receive.Task;
        var failure = new InvalidOperationException("consumer failure");
        Task? notification = null;

        try
        {
            notification = fixture.Owner.NotifyFaultedAsync(fixture.MessageContext, Duration, ConsumerType, failure, caller.Token);
            Assert.False(notification.IsCompleted);
            AssertGeneratedFault(fixture.Owner, fixture.MessageContext, failure);
            AssertNoReceiveEffects(fixture);
            if (cancel)
                caller.Cancel();
            Assert.False(notification.IsCompleted);
            AssertNoReceiveEffects(fixture);

            generation.TrySetResult();
            if (cancel)
            {
                OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(
                    () => notification.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
                Assert.Equal(caller.Token, canceled.CancellationToken);
                Assert.True(notification.IsCanceled);
                AssertReceiveCall(fixture.Receive.Calls, fixture.MessageContext, true, failure, caller.Token);
                Assert.Empty(fixture.Observer.Calls);
                Assert.False(fixture.Receive.IsFaulted);
                Assert.False(fixture.Receive.TryGetPayload(out ConsumerFaultContext? _));
            }
            else
            {
                await fixture.Observer.Invoked.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
                Assert.False(notification.IsCompleted);
                AssertReceiveCall(fixture.Receive.Calls, fixture.MessageContext, true, failure, caller.Token);
                AssertObserverCall(fixture, fixture.MessageContext, true, failure);
                receive.TrySetResult();
                await notification.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
                Assert.True(notification.IsCompletedSuccessfully);
            }
            AssertGeneratedFault(fixture.Owner, fixture.MessageContext, failure);
        }
        finally
        {
            generation.TrySetResult();
            receive.TrySetResult();
            if (notification is not null)
                await ObserveCompletionAsync(notification);
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-CONSUME-NOTIFICATION", "held-generation-failure-wins-over-later-caller-cancellation")]
    public async Task HeldGenerationFailure_PreservesItsOriginalOutcomeAndNeverNotifiesAsync(bool canceledGeneration, bool cancelDelivery)
    {
        using var fixture = new Fixture();
        using var caller = new CancellationTokenSource();
        using var generationCancellation = new CancellationTokenSource();
        generationCancellation.Cancel();
        var generation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Owner.GenerationTask = generation.Task;
        var consumerFailure = new InvalidOperationException("consumer failure");
        var generationFailure = new InvalidOperationException("fault generation failure");
        Task? notification = null;

        try
        {
            notification = fixture.Owner.NotifyFaultedAsync(fixture.MessageContext, Duration, ConsumerType, consumerFailure, caller.Token);
            Assert.False(notification.IsCompleted);
            caller.Cancel();
            if (cancelDelivery)
                fixture.Receive.Cancel();
            Assert.False(notification.IsCompleted);
            AssertNoReceiveEffects(fixture);
            if (canceledGeneration)
            {
                generation.TrySetCanceled(generationCancellation.Token);
                OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(
                    () => notification.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
                Assert.Equal(generationCancellation.Token, canceled.CancellationToken);
                Assert.True(notification.IsCanceled);
            }
            else
            {
                generation.TrySetException(generationFailure);
                InvalidOperationException faulted = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => notification.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
                Assert.Same(generationFailure, faulted);
                Assert.True(notification.IsFaulted);
            }
            AssertGeneratedFault(fixture.Owner, fixture.MessageContext, consumerFailure);
            AssertNoReceiveEffects(fixture);
        }
        finally
        {
            generation.TrySetResult();
            await ObserveCompletionAsync(generation.Task);
            if (notification is not null)
                await ObserveCompletionAsync(notification);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [RequirementCoverage("REQ-VSB-CONSUME-NOTIFICATION", "null-generation-or-receive-task-has-specific-diagnostic")]
    public async Task NullGenerationOrReceiveTask_HasAnOperationSpecificDiagnosticAsync(int boundaryKind)
    {
        using var fixture = new Fixture();
        var boundary = new ReceiveBoundary(fixture) { ReturnedTask = null };
        RecordingContext owner = boundaryKind == 0 ? fixture.Owner : boundary.Owner;
        if (boundaryKind == 0)
            owner.GenerationTask = null;
        var failure = new InvalidOperationException("consumer failure");
        bool faulted = boundaryKind != 2;
        Func<Task> call = () => NotifyAsync(owner, fixture.MessageContext, faulted, ConsumerType, failure,
            TestContext.Current.CancellationToken);

        InvalidOperationException rejected = faulted
            ? await Assert.ThrowsAsync<InvalidOperationException>(call)
            : Assert.Throws<InvalidOperationException>(() => { _ = call(); });
        Assert.Equal(boundaryKind switch
        {
            0 => "The consume context returned no fault-generation task.",
            1 => "The receive context returned no consume-fault notification task.",
            2 => "The receive context returned no consume notification task.",
            _ => throw new ArgumentOutOfRangeException(nameof(boundaryKind)),
        }, rejected.Message);
        if (faulted)
            AssertGeneratedFault(owner, fixture.MessageContext, failure);
        else
            Assert.Empty(owner.GeneratedFaults);
        if (boundaryKind == 0)
            Assert.Empty(boundary.Calls);
        else
            AssertReceiveCall(boundary.Calls, fixture.MessageContext, faulted, failure, TestContext.Current.CancellationToken);
        AssertNoReceiveEffects(fixture);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-CONSUME-NOTIFICATION", "generation-failure-remains-original-without-receive")]
    public async Task GenerationFailure_PreservesTheOriginalExceptionAndNeverNotifiesAsync(bool synchronous)
    {
        using var fixture = new Fixture();
        var consumerFailure = new InvalidOperationException("consumer failure");
        var generationFailure = new InvalidOperationException("fault generation failure");
        if (synchronous)
            fixture.Owner.GenerationException = generationFailure;
        else
            fixture.Owner.GenerationTask = Task.FromException(generationFailure);

        InvalidOperationException faulted = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Owner.NotifyFaultedAsync(fixture.MessageContext, Duration, ConsumerType, consumerFailure,
                TestContext.Current.CancellationToken));
        Assert.Same(generationFailure, faulted);
        AssertGeneratedFault(fixture.Owner, fixture.MessageContext, consumerFailure);
        AssertNoReceiveEffects(fixture);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(4, false)]
    [InlineData(5, true)]
    [InlineData(6, true)]
    [RequirementCoverage("REQ-VSB-CONSUME-NOTIFICATION", "fault-suppression-uses-passed-message-context-token")]
    public async Task FaultSuppression_UsesThePassedMessageContextTokenAsync(int shape, bool generate)
    {
        using var owner = new Fixture();
        using var message = new Fixture();
        if (shape == 4)
            message.Receive.Cancel();
        if (shape is 5 or 6)
            owner.Receive.Cancel();
        Assert.NotEqual(owner.Receive.CancellationToken, message.MessageContext.CancellationToken);
        Exception failure = shape switch
        {
            1 => new OperationCanceledException(message.MessageContext.CancellationToken),
            2 or 6 => new OperationCanceledException(owner.Receive.CancellationToken),
            3 => new OperationCanceledException(CancellationToken.None),
            _ => new InvalidOperationException("consumer failure"),
        };
        using var caller = new CancellationTokenSource();

        await owner.Owner.NotifyFaultedAsync(message.MessageContext, Duration, ConsumerType, failure, caller.Token)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        if (generate)
            AssertGeneratedFault(owner.Owner, message.MessageContext, failure);
        else
            Assert.Empty(owner.Owner.GeneratedFaults);
        AssertReceiveCall(owner.Receive.Calls, message.MessageContext, true, failure, caller.Token);
        AssertObserverCall(owner, message.MessageContext, true, failure);
        AssertNoReceiveEffects(message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-CONSUME-NOTIFICATION", "held-receive-completion-stays-owned-after-caller-cancellation")]
    public async Task HeldReceiveCompletion_RemainsOwnedAfterCallerCancellationAsync(bool faulted)
    {
        using var fixture = new Fixture();
        using var caller = new CancellationTokenSource();
        var receive = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Observer.ReturnedTask = receive.Task;
        var failure = new InvalidOperationException("consumer failure");
        Task? notification = null;

        try
        {
            notification = NotifyAsync(fixture.Owner, fixture.MessageContext, faulted, ConsumerType, failure, caller.Token);
            Assert.False(notification.IsCompleted);
            if (!faulted)
                Assert.Same(receive.Task, notification);
            AssertReceiveCall(fixture.Receive.Calls, fixture.MessageContext, faulted, failure, caller.Token);
            AssertObserverCall(fixture, fixture.MessageContext, faulted, failure);
            caller.Cancel();
            Assert.False(notification.IsCompleted);
            if (faulted)
                AssertGeneratedFault(fixture.Owner, fixture.MessageContext, failure);
            else
                Assert.Empty(fixture.Owner.GeneratedFaults);
        }
        finally
        {
            receive.TrySetResult();
            if (notification is not null)
                await ObserveCompletionAsync(notification);
        }
        Assert.NotNull(notification);
        Assert.True(notification.IsCompletedSuccessfully);
        AssertReceiveCall(fixture.Receive.Calls, fixture.MessageContext, faulted, failure, caller.Token);
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 0)]
    [InlineData(true, 1)]
    [InlineData(true, 2)]
    [RequirementCoverage("REQ-VSB-CONSUME-NOTIFICATION", "receive-failure-or-cancellation-preserves-original-outcome")]
    public async Task ReceiveCompletion_PreservesItsOriginalOutcomeAsync(bool faulted, int outcome)
    {
        using var fixture = new Fixture();
        using var caller = new CancellationTokenSource();
        using var observerCancellation = new CancellationTokenSource();
        observerCancellation.Cancel();
        var consumerFailure = new InvalidOperationException("consumer failure");
        var observerFailure = new InvalidOperationException("observer failure");
        if (outcome == 0)
            fixture.Observer.ThrownException = observerFailure;
        else
            fixture.Observer.ReturnedTask = outcome == 1
                ? Task.FromException(observerFailure)
                : Task.FromCanceled(observerCancellation.Token);
        Func<Task> call = () => NotifyAsync(fixture.Owner, fixture.MessageContext, faulted, ConsumerType, consumerFailure, caller.Token);

        if (outcome == 2)
        {
            Task notification = call();
            OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => notification);
            Assert.Equal(observerCancellation.Token, canceled.CancellationToken);
            Assert.True(notification.IsCanceled);
        }
        else
        {
            InvalidOperationException rejected = !faulted && outcome == 0
                ? Assert.Throws<InvalidOperationException>(() => { _ = call(); })
                : await Assert.ThrowsAsync<InvalidOperationException>(call);
            Assert.Same(observerFailure, rejected);
        }
        if (faulted)
            AssertGeneratedFault(fixture.Owner, fixture.MessageContext, consumerFailure);
        else
            Assert.Empty(fixture.Owner.GeneratedFaults);
        AssertReceiveCall(fixture.Receive.Calls, fixture.MessageContext, faulted, consumerFailure, caller.Token);
        AssertObserverCall(fixture, fixture.MessageContext, faulted, consumerFailure);
    }

    static Task NotifyAsync(RecordingContext owner, ConsumeContext<NotificationMessage> context, bool faulted,
        string consumerType, Exception exception, CancellationToken cancellationToken)
    {
        return faulted
            ? owner.NotifyFaultedAsync(context, Duration, consumerType, exception, cancellationToken)
            : owner.NotifyConsumedAsync(context, Duration, consumerType, cancellationToken);
    }

    static void AssertGeneratedFault(RecordingContext owner, ConsumeContext<NotificationMessage> context, Exception exception)
    {
        GeneratedFault generated = Assert.Single(owner.GeneratedFaults);
        Assert.Same(context, generated.Context);
        Assert.Same(exception, generated.Exception);
    }

    static void AssertNoReceiveEffects(Fixture fixture)
    {
        Assert.Empty(fixture.Receive.Calls);
        Assert.Empty(fixture.Observer.Calls);
        Assert.False(fixture.Receive.IsDelivered);
        Assert.False(fixture.Receive.IsFaulted);
        Assert.False(fixture.Receive.TryGetPayload(out ConsumerFaultContext? _));
    }

    static void AssertReceiveCall(List<ReceiveCall> calls, ConsumeContext<NotificationMessage> context, bool faulted,
        Exception failure, CancellationToken cancellationToken)
    {
        ReceiveCall call = Assert.Single(calls);
        Assert.Same(context, call.Context);
        Assert.Equal(faulted, call.Faulted);
        Assert.Equal(Duration, call.Duration);
        Assert.Equal(ConsumerType, call.ConsumerType);
        Assert.Same(faulted ? failure : null, call.Exception);
        Assert.Equal(cancellationToken, call.CancellationToken);
    }

    static void AssertObserverCall(Fixture fixture, ConsumeContext<NotificationMessage> context, bool faulted, Exception failure)
    {
        ObserverCall call = Assert.Single(fixture.Observer.Calls);
        Assert.Same(context, call.Context);
        Assert.Equal(faulted, call.Faulted);
        Assert.Equal(Duration, call.Duration);
        Assert.Equal(ConsumerType, call.ConsumerType);
        Assert.Same(faulted ? failure : null, call.Exception);
        Assert.Equal(!faulted, call.IsDelivered);
        Assert.Equal(faulted, call.IsFaulted);
        Assert.Equal(!faulted, fixture.Receive.IsDelivered);
        Assert.Equal(faulted, fixture.Receive.IsFaulted);
        Assert.Equal(faulted, fixture.Receive.TryGetPayload(out ConsumerFaultContext? _));
    }

    static async Task ObserveCompletionAsync(Task task)
    {
        try
        {
            await task.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
        }
        catch (Exception) when (task.IsCompleted)
        {
            // Cleanup observes settled failures without replacing assertions; incomplete work still times out.
        }
    }

    sealed class Fixture : IDisposable
    {
        public Fixture()
        {
            Observer = new RecordingObserver();
            var message = new NotificationMessage("notification");
            var send = new MessageSendContext<NotificationMessage>(message, TestContext.Current.CancellationToken)
            {
                DestinationAddress = new Uri("loopback://localhost/consume-notification"),
            };
            IPublishEndpointProvider publish = StrictProxy.CreateUnused<IPublishEndpointProvider>();
            ReceiveEndpointContext endpoint = StrictProxy.Create<ReceiveEndpointContext>((method, args) => method.Name switch
            {
                "get_InputAddress" => send.DestinationAddress,
                "get_ReceiveObservers" => Observer,
                "get_PublishEndpointProvider" => publish,
                nameof(PipeContext.TryGetPayload) => method.Invoke(send, args),
                _ => throw new InvalidOperationException($"Unexpected endpoint operation: {method.Name}."),
            });
            Receive = new TransportContext(endpoint);
            Observer.Receive = Receive;
            var metadata = new MediatorSendMessageContext<NotificationMessage>(send);
            Serialization = new MediatorSerializationContext<NotificationMessage>(
                ServiceBusMetadataJson.ObjectDeserializer, metadata, message, send.SupportedMessageTypes);
            MessageContext = new MediatorConsumeContext<NotificationMessage>(Receive, Serialization, message);
            Owner = new RecordingContext(Receive, Serialization);
        }

        public TransportContext Receive { get; }
        public SerializerContext Serialization { get; }
        public ConsumeContext<NotificationMessage> MessageContext { get; }
        public RecordingContext Owner { get; }
        public RecordingObserver Observer { get; }
        public void Dispose() => Receive.Dispose();
    }

    sealed class RecordingContext(ReceiveContext receive, SerializerContext serialization) : DeserializerConsumeContext(receive, serialization)
    {
        public Task? GenerationTask { get; set; } = Task.CompletedTask;
        public Exception? GenerationException { get; set; }
        public List<GeneratedFault> GeneratedFaults { get; } = [];
        public override Guid? MessageId => SerializerContext.MessageId;
        public override Guid? RequestId => SerializerContext.RequestId;
        public override Guid? CorrelationId => SerializerContext.CorrelationId;
        public override Guid? ConversationId => SerializerContext.ConversationId;
        public override Guid? InitiatorId => SerializerContext.InitiatorId;
        public override DateTimeOffset? ExpirationTime => SerializerContext.ExpirationTime;
        public override Uri? SourceAddress => SerializerContext.SourceAddress;
        public override Uri? DestinationAddress => SerializerContext.DestinationAddress;
        public override Uri? ResponseAddress => SerializerContext.ResponseAddress;
        public override Uri? FaultAddress => SerializerContext.FaultAddress;
        public override DateTimeOffset? SentTime => SerializerContext.SentTime;
        public override Headers Headers => SerializerContext.Headers;
        public override HostInfo Host => SerializerContext.Host;
        public override IEnumerable<string> SupportedMessageTypes => SerializerContext.SupportedMessageTypes;
        public override bool HasMessageType(Type messageType) => SerializerContext.IsSupportedMessageType(messageType);
        public override bool TryGetMessage<T>([NotNullWhen(true)] out ConsumeContext<T>? consumeContext)
            where T : class
        {
            if (SerializerContext.TryGetMessage(out T? message))
            {
                consumeContext = new MessageConsumeContext<T>(this, message);
                return true;
            }
            consumeContext = null;
            return false;
        }

        protected override Task GenerateFaultAsync<T>(ConsumeContext<T> context, Exception exception)
        {
            GeneratedFaults.Add(new GeneratedFault(context, exception));
            if (GenerationException is { } failure)
                throw failure;
            return GenerationTask!;
        }
    }

    sealed class TransportContext(ReceiveEndpointContext endpoint) : BaseReceiveContext(false, endpoint)
    {
        public List<ReceiveCall> Calls { get; } = [];
        protected override IHeaderProvider HeaderProvider => new DictionarySendHeaderProvider(new DictionarySendHeaders());
        public override MessageBody Body => new BinaryMessageBody("{}"u8.ToArray());

        public override Task NotifyConsumedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType,
            CancellationToken cancellationToken = default)
        {
            Calls.Add(new ReceiveCall(context, false, duration, consumerType, null, cancellationToken));
            return base.NotifyConsumedAsync(context, duration, consumerType, cancellationToken);
        }

        public override Task NotifyFaultedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception,
            CancellationToken cancellationToken = default)
        {
            Calls.Add(new ReceiveCall(context, true, duration, consumerType, exception, cancellationToken));
            return base.NotifyFaultedAsync(context, duration, consumerType, exception, cancellationToken);
        }
    }

    sealed class ReceiveBoundary
    {
        public ReceiveBoundary(Fixture fixture)
        {
            ReceiveContext receive = StrictProxy.Create<ReceiveContext>((method, args) =>
            {
                if (method.Name == "get_PublishEndpointProvider")
                    return fixture.Receive.PublishEndpointProvider;
                if (method.Name is nameof(ReceiveContext.NotifyConsumedAsync) or nameof(ReceiveContext.NotifyFaultedAsync))
                {
                    bool faulted = method.Name == nameof(ReceiveContext.NotifyFaultedAsync);
                    Calls.Add(new ReceiveCall(args![0]!, faulted, (TimeSpan)args[1]!, (string)args[2]!,
                        faulted ? (Exception)args[3]! : null, (CancellationToken)args[faulted ? 4 : 3]!));
                    return ReturnedTask;
                }
                throw new InvalidOperationException($"Unexpected receive boundary operation: {method.Name}.");
            });
            Owner = new RecordingContext(receive, fixture.Serialization);
        }

        public RecordingContext Owner { get; }
        public List<ReceiveCall> Calls { get; } = [];
        public Task? ReturnedTask { get; set; } = Task.CompletedTask;
    }

    sealed class RecordingObserver : IReceiveObserver
    {
        public TransportContext Receive { get; set; } = null!;
        public Task? ReturnedTask { get; set; } = Task.CompletedTask;
        public Exception? ThrownException { get; set; }
        public List<ObserverCall> Calls { get; } = [];
        public TaskCompletionSource Invoked { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task PreReceiveAsync(ReceiveContext context) => throw new InvalidOperationException("Unexpected pre-receive notification.");
        public Task PostReceiveAsync(ReceiveContext context) => throw new InvalidOperationException("Unexpected post-receive notification.");
        public Task ReceiveFaultAsync(ReceiveContext context, Exception exception) => throw new InvalidOperationException("Unexpected receive fault.");

        public Task PostConsumeAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType)
            where T : class => RecordAsync(context, false, duration, consumerType, null);

        public Task ConsumeFaultAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception)
            where T : class => RecordAsync(context, true, duration, consumerType, exception);

        Task RecordAsync(object context, bool faulted, TimeSpan duration, string consumerType, Exception? exception)
        {
            Calls.Add(new ObserverCall(context, faulted, duration, consumerType, exception, Receive.IsDelivered, Receive.IsFaulted));
            Invoked.TrySetResult();
            if (ThrownException is { } failure)
                throw failure;
            return ReturnedTask!;
        }
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
            where T : class => Create<T>((method, _) => throw new InvalidOperationException($"Unexpected dependency operation: {method.Name}."));
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            return (_dispatch ?? throw new InvalidOperationException("Dependency dispatch is not configured."))(targetMethod, args);
        }
    }

    sealed record NotificationMessage(string Value);
    sealed record GeneratedFault(object Context, Exception Exception);
    sealed record ReceiveCall(object Context, bool Faulted, TimeSpan Duration, string ConsumerType, Exception? Exception, CancellationToken CancellationToken);
    sealed record ObserverCall(object Context, bool Faulted, TimeSpan Duration, string ConsumerType, Exception? Exception, bool IsDelivered, bool IsFaulted);
}
