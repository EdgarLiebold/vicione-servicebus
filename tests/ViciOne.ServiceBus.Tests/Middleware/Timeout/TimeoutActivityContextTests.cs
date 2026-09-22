using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Middleware.Timeout;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware.Timeout;

public sealed class TimeoutActivityContextTests
{
    static readonly TimeSpan ConfiguredTimeout = TimeSpan.FromSeconds(17);
    static readonly TimeSpan Duration = TimeSpan.FromMilliseconds(325);
    const string ConsumerType = "timeout-activity-filter";

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVITY-TIMEOUT", "timeout-token-cancellation-is-reported-as-configured-timeout")]
    public async Task TimeoutCancellation_GeneratesAndNotifiesTheConfiguredTimeoutFaultAsync()
    {
        using var timeout = new CancellationTokenSource();
        using var caller = new CancellationTokenSource();
        var fixture = new Fixture(CancellationToken.None, timeout.Token);
        timeout.Cancel();
        var cancellation = new OperationCanceledException("activity timed out", null, timeout.Token);

        await fixture.Context.NotifyFaultedAsync(
            fixture.MessageContext,
            Duration,
            ConsumerType,
            cancellation,
            caller.Token);

        GeneratedFault generatedFault = Assert.Single(fixture.Context.GeneratedFaults);
        Assert.Same(fixture.MessageContext, generatedFault.Context);
        ConsumerCanceledException generated = Assert.IsType<ConsumerCanceledException>(generatedFault.Exception);
        Assert.Contains(ConfiguredTimeout.ToString(), generated.Message, StringComparison.Ordinal);
        Assert.Same(cancellation, generated.InnerException);
        AssertNotification(fixture, generated, caller.Token);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVITY-TIMEOUT", "independent-cancellation-is-not-reclassified-after-timeout")]
    public async Task IndependentCancellation_AfterTimeoutElapsed_RemainsTheOriginalFaultAsync()
    {
        using var timeout = new CancellationTokenSource();
        using var independent = new CancellationTokenSource();
        timeout.Cancel();
        independent.Cancel();
        var fixture = new Fixture(CancellationToken.None, timeout.Token);
        var cancellation = new OperationCanceledException("independent cancellation", null, independent.Token);

        await fixture.Context.NotifyFaultedAsync(
            fixture.MessageContext,
            Duration,
            ConsumerType,
            cancellation,
            TestContext.Current.CancellationToken);

        Assert.Same(cancellation, Assert.Single(fixture.Context.GeneratedFaults).Exception);
        Assert.Same(cancellation, Assert.Single(fixture.Receive.Notifications).Exception);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVITY-TIMEOUT", "matching-but-active-timeout-token-is-not-a-timeout")]
    public async Task CancellationWithAnActiveTimeoutToken_RemainsTheOriginalFaultAsync()
    {
        using var timeout = new CancellationTokenSource();
        var fixture = new Fixture(CancellationToken.None, timeout.Token);
        var cancellation = new OperationCanceledException("token is not canceled", null, timeout.Token);

        await fixture.Context.NotifyFaultedAsync(
            fixture.MessageContext,
            Duration,
            ConsumerType,
            cancellation,
            TestContext.Current.CancellationToken);

        Assert.Same(cancellation, Assert.Single(fixture.Context.GeneratedFaults).Exception);
        Assert.Same(cancellation, Assert.Single(fixture.Receive.Notifications).Exception);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVITY-TIMEOUT", "matching-active-message-token-suppresses-fault-generation")]
    public async Task CancellationWithTheActiveMessageToken_SuppressesGenerationAndStillNotifiesAsync()
    {
        using var message = new CancellationTokenSource();
        var fixture = new Fixture(message.Token, CancellationToken.None);
        var cancellation = new OperationCanceledException("message token is still active", null, message.Token);

        await fixture.Context.NotifyFaultedAsync(
            fixture.MessageContext,
            Duration,
            ConsumerType,
            cancellation,
            TestContext.Current.CancellationToken);

        Assert.Empty(fixture.Context.GeneratedFaults);
        Assert.Same(cancellation, Assert.Single(fixture.Receive.Notifications).Exception);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVITY-TIMEOUT", "delivery-cancellation-suppresses-fault-publication")]
    public async Task DeliveryCancellation_SuppressesGenerationButStillNotifiesWithTheOriginalFailureAsync()
    {
        using var delivery = new CancellationTokenSource();
        delivery.Cancel();
        var fixture = new Fixture(delivery.Token, CancellationToken.None);
        var cancellation = new OperationCanceledException("delivery stopped", null, delivery.Token);

        await fixture.Context.NotifyFaultedAsync(
            fixture.MessageContext,
            Duration,
            ConsumerType,
            cancellation,
            TestContext.Current.CancellationToken);

        Assert.Empty(fixture.Context.GeneratedFaults);
        Assert.Same(cancellation, Assert.Single(fixture.Receive.Notifications).Exception);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVITY-TIMEOUT", "stopped-delivery-suppresses-independent-fault-publication")]
    public async Task StoppedDelivery_SuppressesAnIndependentFaultButStillNotifiesAsync()
    {
        using var delivery = new CancellationTokenSource();
        delivery.Cancel();
        var fixture = new Fixture(delivery.Token, CancellationToken.None);
        var failure = new InvalidOperationException("activity failed while delivery stopped");

        await fixture.Context.NotifyFaultedAsync(
            fixture.MessageContext,
            Duration,
            ConsumerType,
            failure,
            TestContext.Current.CancellationToken);

        Assert.Empty(fixture.Context.GeneratedFaults);
        Assert.Same(failure, Assert.Single(fixture.Receive.Notifications).Exception);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVITY-TIMEOUT", "message-context-token-is-authoritative-over-owner-token")]
    public async Task CanceledOwner_DoesNotSuppressFaultGenerationForAnActiveMessageContextAsync()
    {
        using var owner = new CancellationTokenSource();
        owner.Cancel();
        var fixture = new Fixture(CancellationToken.None, CancellationToken.None, owner.Token);
        var failure = new InvalidOperationException("message context remains active");

        await fixture.Context.NotifyFaultedAsync(
            fixture.MessageContext,
            Duration,
            ConsumerType,
            failure,
            TestContext.Current.CancellationToken);

        Assert.Same(failure, Assert.Single(fixture.Context.GeneratedFaults).Exception);
        Assert.Same(failure, Assert.Single(fixture.Receive.Notifications).Exception);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVITY-TIMEOUT", "ordinary-fault-generation-completes-before-notification")]
    public async Task OrdinaryFailure_IsGeneratedBeforeTheReceivePipelineIsNotifiedAsync()
    {
        var fixture = new Fixture(CancellationToken.None, CancellationToken.None);
        var generation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Context.FaultGenerationTask = generation.Task;
        var failure = new InvalidOperationException("activity failed");

        Task notification = fixture.Context.NotifyFaultedAsync(
            fixture.MessageContext,
            Duration,
            ConsumerType,
            failure,
            TestContext.Current.CancellationToken);

        Assert.Same(failure, Assert.Single(fixture.Context.GeneratedFaults).Exception);
        Assert.Empty(fixture.Receive.Notifications);
        Assert.False(notification.IsCompleted);

        generation.SetResult();
        await notification;

        Assert.Same(failure, Assert.Single(fixture.Receive.Notifications).Exception);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVITY-TIMEOUT", "receive-notification-completion-is-owned")]
    public async Task ReceiveNotification_IsAwaitedBeforeTheReturnedTaskCompletesAsync()
    {
        var fixture = new Fixture(CancellationToken.None, CancellationToken.None);
        var receiveCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Receive.NotificationTask = receiveCompletion.Task;

        Task notification = fixture.Context.NotifyFaultedAsync(
            fixture.MessageContext,
            Duration,
            ConsumerType,
            new InvalidOperationException("activity failed"),
            TestContext.Current.CancellationToken);

        Assert.Single(fixture.Context.GeneratedFaults);
        Assert.Single(fixture.Receive.Notifications);
        Assert.False(notification.IsCompleted);

        receiveCompletion.SetResult();
        await notification;
        Assert.True(notification.IsCompletedSuccessfully);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVITY-TIMEOUT", "receive-notification-fault-is-preserved")]
    public async Task ReceiveNotificationFault_PreservesTheExactFailureAsync()
    {
        var fixture = new Fixture(CancellationToken.None, CancellationToken.None);
        var receiveFailure = new InvalidOperationException("receive notification failed");
        fixture.Receive.NotificationTask = Task.FromException(receiveFailure);

        Task notification = fixture.Context.NotifyFaultedAsync(
            fixture.MessageContext,
            Duration,
            ConsumerType,
            new InvalidOperationException("activity failed"),
            TestContext.Current.CancellationToken);

        Assert.Same(receiveFailure, await Assert.ThrowsAsync<InvalidOperationException>(() => notification));
        Assert.Single(fixture.Context.GeneratedFaults);
        Assert.Single(fixture.Receive.Notifications);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVITY-TIMEOUT", "receive-notification-cancellation-is-preserved")]
    public async Task ReceiveNotificationCancellation_PreservesTheExactTokenAsync()
    {
        var fixture = new Fixture(CancellationToken.None, CancellationToken.None);
        using var receiveCancellation = new CancellationTokenSource();
        receiveCancellation.Cancel();
        fixture.Receive.NotificationTask = Task.FromCanceled(receiveCancellation.Token);

        Task notification = fixture.Context.NotifyFaultedAsync(
            fixture.MessageContext,
            Duration,
            ConsumerType,
            new InvalidOperationException("activity failed"),
            TestContext.Current.CancellationToken);

        OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => notification);
        Assert.Equal(receiveCancellation.Token, canceled.CancellationToken);
        Assert.Single(fixture.Context.GeneratedFaults);
        Assert.Single(fixture.Receive.Notifications);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVITY-TIMEOUT", "caller-cancellation-after-receive-starts-does-not-replace-receive-outcome")]
    public async Task CallerCancellationAfterReceiveStarts_DoesNotReplaceTheReceiveFailureAsync()
    {
        var fixture = new Fixture(CancellationToken.None, CancellationToken.None);
        using var caller = new CancellationTokenSource();
        var receiveCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var receiveFailure = new InvalidOperationException("receive notification failed after caller cancellation");
        fixture.Receive.NotificationTask = receiveCompletion.Task;

        Task notification = fixture.Context.NotifyFaultedAsync(
            fixture.MessageContext,
            Duration,
            ConsumerType,
            new InvalidOperationException("activity failed"),
            caller.Token);
        Assert.Single(fixture.Receive.Notifications);

        caller.Cancel();
        Assert.False(notification.IsCompleted);
        receiveCompletion.SetException(receiveFailure);

        Assert.Same(receiveFailure, await Assert.ThrowsAsync<InvalidOperationException>(() => notification));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVITY-TIMEOUT", "null-fault-generation-task-is-rejected-before-notification")]
    public async Task MissingFaultGenerationTask_IsRejectedBeforeReceiveNotificationAsync()
    {
        var fixture = new Fixture(CancellationToken.None, CancellationToken.None);
        fixture.Context.FaultGenerationTask = null!;

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Context.NotifyFaultedAsync(
                fixture.MessageContext,
                Duration,
                ConsumerType,
                new InvalidOperationException("activity failed"),
                TestContext.Current.CancellationToken));

        Assert.Equal("The consume context returned no fault-generation task.", exception.Message);
        Assert.Single(fixture.Context.GeneratedFaults);
        Assert.Empty(fixture.Receive.Notifications);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVITY-TIMEOUT", "fault-generation-fault-is-preserved")]
    public async Task FaultGenerationFailure_PreservesTheExactFailureAndSkipsReceiveAsync()
    {
        var fixture = new Fixture(CancellationToken.None, CancellationToken.None);
        var generationFailure = new InvalidOperationException("fault generation failed");
        fixture.Context.FaultGenerationTask = Task.FromException(generationFailure);

        Task notification = fixture.Context.NotifyFaultedAsync(
            fixture.MessageContext,
            Duration,
            ConsumerType,
            new InvalidOperationException("activity failed"),
            TestContext.Current.CancellationToken);

        Assert.Same(generationFailure, await Assert.ThrowsAsync<InvalidOperationException>(() => notification));
        Assert.Single(fixture.Context.GeneratedFaults);
        Assert.Empty(fixture.Receive.Notifications);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVITY-TIMEOUT", "fault-generation-cancellation-is-preserved")]
    public async Task FaultGenerationCancellation_PreservesTheExactTokenAndSkipsReceiveAsync()
    {
        var fixture = new Fixture(CancellationToken.None, CancellationToken.None);
        using var generationCancellation = new CancellationTokenSource();
        generationCancellation.Cancel();
        fixture.Context.FaultGenerationTask = Task.FromCanceled(generationCancellation.Token);

        Task notification = fixture.Context.NotifyFaultedAsync(
            fixture.MessageContext,
            Duration,
            ConsumerType,
            new InvalidOperationException("activity failed"),
            TestContext.Current.CancellationToken);

        OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => notification);
        Assert.Equal(generationCancellation.Token, canceled.CancellationToken);
        Assert.Single(fixture.Context.GeneratedFaults);
        Assert.Empty(fixture.Receive.Notifications);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVITY-TIMEOUT", "caller-cancellation-after-generation-starts-does-not-replace-generation-fault")]
    public async Task CallerCancellationAfterGenerationStarts_DoesNotReplaceTheGenerationFailureAsync()
    {
        var fixture = new Fixture(CancellationToken.None, CancellationToken.None);
        using var caller = new CancellationTokenSource();
        var generation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var generationFailure = new InvalidOperationException("fault generation failed after caller cancellation");
        fixture.Context.FaultGenerationTask = generation.Task;

        Task notification = fixture.Context.NotifyFaultedAsync(
            fixture.MessageContext,
            Duration,
            ConsumerType,
            new InvalidOperationException("activity failed"),
            caller.Token);
        caller.Cancel();
        generation.SetException(generationFailure);

        Assert.Same(generationFailure, await Assert.ThrowsAsync<InvalidOperationException>(() => notification));
        Assert.Empty(fixture.Receive.Notifications);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVITY-TIMEOUT", "caller-cancellation-after-generation-starts-does-not-replace-generation-cancellation")]
    public async Task CallerCancellationAfterGenerationStarts_DoesNotReplaceTheGenerationCancellationAsync()
    {
        var fixture = new Fixture(CancellationToken.None, CancellationToken.None);
        using var caller = new CancellationTokenSource();
        using var generationCancellation = new CancellationTokenSource();
        var generation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Context.FaultGenerationTask = generation.Task;

        Task notification = fixture.Context.NotifyFaultedAsync(
            fixture.MessageContext,
            Duration,
            ConsumerType,
            new InvalidOperationException("activity failed"),
            caller.Token);
        caller.Cancel();
        generationCancellation.Cancel();
        generation.SetCanceled(generationCancellation.Token);

        OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => notification);
        Assert.Equal(generationCancellation.Token, canceled.CancellationToken);
        Assert.Empty(fixture.Receive.Notifications);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVITY-TIMEOUT", "caller-cancellation-after-generation-starts-is-forwarded-to-receive")]
    public async Task CallerCancellationAfterGenerationStarts_DoesNotReplaceGenerationOrSkipReceiveAsync()
    {
        var fixture = new Fixture(CancellationToken.None, CancellationToken.None);
        using var caller = new CancellationTokenSource();
        var generation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Context.FaultGenerationTask = generation.Task;

        Task notification = fixture.Context.NotifyFaultedAsync(
            fixture.MessageContext,
            Duration,
            ConsumerType,
            new InvalidOperationException("activity failed"),
            caller.Token);
        caller.Cancel();
        generation.SetResult();
        await notification;

        Assert.Equal(caller.Token, Assert.Single(fixture.Receive.Notifications).CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVITY-TIMEOUT", "null-receive-notification-task-is-rejected")]
    public async Task MissingReceiveNotificationTask_IsRejectedAfterFaultGenerationAsync()
    {
        var fixture = new Fixture(CancellationToken.None, CancellationToken.None);
        fixture.Receive.NotificationTask = null;

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Context.NotifyFaultedAsync(
                fixture.MessageContext,
                Duration,
                ConsumerType,
                new InvalidOperationException("activity failed"),
                TestContext.Current.CancellationToken));

        Assert.Equal("The receive context returned no consume-fault notification task.", exception.Message);
        Assert.Single(fixture.Context.GeneratedFaults);
        Assert.Single(fixture.Receive.Notifications);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVITY-TIMEOUT", "caller-cancellation-has-no-fault-side-effects")]
    public async Task CanceledCaller_ReturnsTheExactCancellationWithoutGeneratingOrNotifyingAsync()
    {
        var fixture = new Fixture(CancellationToken.None, CancellationToken.None);
        using var caller = new CancellationTokenSource();
        caller.Cancel();

        Task notification = fixture.Context.NotifyFaultedAsync(
            fixture.MessageContext,
            Duration,
            ConsumerType,
            new InvalidOperationException("must not be observed"),
            caller.Token);

        OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => notification);
        Assert.Equal(caller.Token, canceled.CancellationToken);
        Assert.Empty(fixture.Context.GeneratedFaults);
        Assert.Empty(fixture.Receive.Notifications);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVITY-TIMEOUT", "required-arguments-are-validated-before-cancellation")]
    public void InvalidArguments_AreRejectedSynchronouslyBeforeCancellationWithoutSideEffects()
    {
        var fixture = new Fixture(CancellationToken.None, CancellationToken.None);
        using var caller = new CancellationTokenSource();
        caller.Cancel();
        var failure = new InvalidOperationException("activity failed");

        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = fixture.Context.NotifyFaultedAsync<ProbeMessage>(null!, Duration, ConsumerType, failure, caller.Token);
        }).ParamName);
        Assert.Equal("consumerType", Assert.Throws<ArgumentException>(() =>
        {
            _ = fixture.Context.NotifyFaultedAsync(fixture.MessageContext, Duration, " ", failure, caller.Token);
        }).ParamName);
        Assert.Equal("exception", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = fixture.Context.NotifyFaultedAsync(fixture.MessageContext, Duration, ConsumerType, null!, caller.Token);
        }).ParamName);
        Assert.Empty(fixture.Context.GeneratedFaults);
        Assert.Empty(fixture.Receive.Notifications);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [RequirementCoverage("REQ-VSB-ACTIVITY-TIMEOUT", "timeout-must-be-positive")]
    public void Constructor_RejectsNonpositiveTimeout(int milliseconds)
    {
        ActivityContext source = CreateActivityContext(CancellationToken.None, out _);

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RecordingTimeoutActivityContext(source, CancellationToken.None, TimeSpan.FromMilliseconds(milliseconds)));

        Assert.Equal("timeout", exception.ParamName);
        Assert.Equal(TimeSpan.FromMilliseconds(milliseconds), exception.ActualValue);
    }

    static ActivityContext CreateActivityContext(CancellationToken cancellationToken, out RecordingReceiveContextProxy receive)
    {
        ReceiveContext receiveContext = DispatchProxy.Create<ReceiveContext, RecordingReceiveContextProxy>();
        receive = (RecordingReceiveContextProxy)(object)receiveContext;
        ActivityContext context = DispatchProxy.Create<ActivityContext, ActivityContextState>();
        var state = (ActivityContextState)(object)context;
        state.CancellationToken = cancellationToken;
        state.ReceiveContext = receiveContext;
        state.SerializerContext = DispatchProxy.Create<SerializerContext, UnexpectedInvocationProxy>();
        return context;
    }

    static void AssertNotification(Fixture fixture, Exception expectedException, CancellationToken expectedCancellationToken)
    {
        FaultNotification notified = Assert.Single(fixture.Receive.Notifications);
        Assert.Same(fixture.MessageContext, notified.Context);
        Assert.Equal(Duration, notified.Duration);
        Assert.Equal(ConsumerType, notified.ConsumerType);
        Assert.Same(expectedException, notified.Exception);
        Assert.Equal(expectedCancellationToken, notified.CancellationToken);
    }

    sealed class Fixture
    {
        public Fixture(CancellationToken messageToken, CancellationToken timeoutToken, CancellationToken ownerToken = default)
        {
            ActivityContext source = CreateActivityContext(ownerToken, out RecordingReceiveContextProxy receive);
            Receive = receive;
            Context = new RecordingTimeoutActivityContext(source, timeoutToken, ConfiguredTimeout);
            MessageContext = DispatchProxy.Create<ConsumeContext<ProbeMessage>, MessageContextState>();
            ((MessageContextState)(object)MessageContext).CancellationToken = messageToken;
        }

        public RecordingTimeoutActivityContext Context { get; }
        public ConsumeContext<ProbeMessage> MessageContext { get; }
        public RecordingReceiveContextProxy Receive { get; }
    }

    sealed record ProbeMessage;

    sealed record GeneratedFault(object Context, Exception Exception);

    sealed record FaultNotification(
        object Context,
        TimeSpan Duration,
        string ConsumerType,
        Exception Exception,
        CancellationToken CancellationToken);

    sealed class RecordingTimeoutActivityContext(ActivityContext context, CancellationToken cancellationToken, TimeSpan timeout)
        : TimeoutActivityContextProxy(context, cancellationToken, timeout)
    {
        public Task FaultGenerationTask { get; set; } = Task.CompletedTask;

        public List<GeneratedFault> GeneratedFaults { get; } = [];

        protected override Task GenerateFaultAsync<T>(ConsumeContext<T> context, Exception exception)
        {
            GeneratedFaults.Add(new GeneratedFault(context, exception));
            return FaultGenerationTask;
        }
    }

    class ActivityContextState : DispatchProxy
    {
        public CancellationToken CancellationToken { get; set; }

        public ReceiveContext ReceiveContext { get; set; } = null!;

        public SerializerContext SerializerContext { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_CancellationToken" => CancellationToken,
            "get_ReceiveContext" => ReceiveContext,
            "get_SerializerContext" => SerializerContext,
            _ => throw new InvalidOperationException($"The timeout activity source unexpectedly invoked {targetMethod?.Name}."),
        };
    }

    class RecordingReceiveContextProxy : DispatchProxy
    {
        public List<FaultNotification> Notifications { get; } = [];

        public Task? NotificationTask { get; set; } = Task.CompletedTask;

        public IPublishEndpointProvider PublishEndpointProvider { get; } =
            DispatchProxy.Create<IPublishEndpointProvider, UnexpectedInvocationProxy>();

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            MethodInfo method = targetMethod ?? throw new InvalidOperationException("The receive context supplied no method metadata.");
            if (method.Name == "get_PublishEndpointProvider")
                return PublishEndpointProvider;
            if (method.Name == nameof(ReceiveContext.NotifyFaultedAsync) && args?.Length == 5)
            {
                Notifications.Add(new FaultNotification(
                    args[0]!,
                    (TimeSpan)args[1]!,
                    (string)args[2]!,
                    (Exception)args[3]!,
                    (CancellationToken)args[4]!));
                return NotificationTask;
            }

            throw new InvalidOperationException($"The receive context unexpectedly invoked {method.Name}.");
        }
    }

    class MessageContextState : DispatchProxy
    {
        public CancellationToken CancellationToken { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_CancellationToken" => CancellationToken,
            _ => throw new InvalidOperationException($"The message context unexpectedly invoked {targetMethod?.Name}."),
        };
    }

    class UnexpectedInvocationProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"The timeout activity test unexpectedly invoked {targetMethod?.Name}.");
    }
}
