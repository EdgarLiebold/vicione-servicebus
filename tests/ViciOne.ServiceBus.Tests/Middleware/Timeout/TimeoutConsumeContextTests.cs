using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Middleware.Timeout;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware.Timeout;

public sealed class TimeoutConsumeContextTests
{
    static readonly TimeSpan ConfiguredTimeout = TimeSpan.FromSeconds(23);
    static readonly TimeSpan Duration = TimeSpan.FromMilliseconds(417);
    const string ConsumerType = "timeout-consume-filter";

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUME-TIMEOUT", "timeout-token-cancellation-is-reported-for-the-passed-context")]
    public async Task TimeoutCancellation_GeneratesAndNotifiesForThePassedContextAsync()
    {
        using var timeout = new CancellationTokenSource();
        timeout.Cancel();
        var fixture = new Fixture(CancellationToken.None, CancellationToken.None, timeout.Token);
        var cancellation = new OperationCanceledException("consume timed out", null, timeout.Token);

        await fixture.Context.NotifyFaultedAsync(
            fixture.MessageContext,
            Duration,
            ConsumerType,
            cancellation,
            TestContext.Current.CancellationToken);

        GeneratedFault generatedFault = Assert.Single(fixture.Context.GeneratedFaults);
        Assert.Same(fixture.MessageContext, generatedFault.Context);
        ConsumerCanceledException reported = Assert.IsType<ConsumerCanceledException>(generatedFault.Exception);
        Assert.Contains(ConfiguredTimeout.ToString(), reported.Message, StringComparison.Ordinal);
        Assert.Same(cancellation, reported.InnerException);
        AssertNotification(fixture, reported, TestContext.Current.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUME-TIMEOUT", "independent-cancellation-is-not-reclassified-after-timeout")]
    public async Task IndependentCancellation_AfterTimeoutElapsed_RemainsTheOriginalFaultAsync()
    {
        using var timeout = new CancellationTokenSource();
        using var independent = new CancellationTokenSource();
        timeout.Cancel();
        independent.Cancel();
        var fixture = new Fixture(CancellationToken.None, CancellationToken.None, timeout.Token);
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
    [RequirementCoverage("REQ-VSB-CONSUME-TIMEOUT", "matching-active-timeout-token-is-not-a-timeout")]
    public async Task CancellationWithAnActiveTimeoutToken_RemainsTheOriginalFaultAsync()
    {
        using var timeout = new CancellationTokenSource();
        var fixture = new Fixture(CancellationToken.None, CancellationToken.None, timeout.Token);
        var cancellation = new OperationCanceledException("timeout token is active", null, timeout.Token);

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
    [RequirementCoverage("REQ-VSB-CONSUME-TIMEOUT", "active-matching-message-token-suppresses-generation")]
    public async Task CancellationWithTheActiveMessageToken_SuppressesGenerationAndStillNotifiesAsync()
    {
        using var message = new CancellationTokenSource();
        var fixture = new Fixture(CancellationToken.None, message.Token, CancellationToken.None);
        var cancellation = new OperationCanceledException("message token is active", null, message.Token);

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
    [RequirementCoverage("REQ-VSB-CONSUME-TIMEOUT", "canceled-message-context-suppresses-ordinary-fault-generation")]
    public async Task CanceledMessageContext_SuppressesAnOrdinaryFaultButStillNotifiesAsync()
    {
        using var message = new CancellationTokenSource();
        message.Cancel();
        var fixture = new Fixture(CancellationToken.None, message.Token, CancellationToken.None);
        var failure = new InvalidOperationException("delivery stopped");

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
    [RequirementCoverage("REQ-VSB-CONSUME-TIMEOUT", "passed-message-context-is-authoritative-over-owner")]
    public async Task CanceledOwner_DoesNotSuppressFaultForAnActivePassedContextAsync()
    {
        using var owner = new CancellationTokenSource();
        owner.Cancel();
        var fixture = new Fixture(owner.Token, CancellationToken.None, CancellationToken.None);
        var failure = new InvalidOperationException("passed context is active");

        await fixture.Context.NotifyFaultedAsync(
            fixture.MessageContext,
            Duration,
            ConsumerType,
            failure,
            TestContext.Current.CancellationToken);

        GeneratedFault generated = Assert.Single(fixture.Context.GeneratedFaults);
        Assert.Same(fixture.MessageContext, generated.Context);
        Assert.Same(failure, generated.Exception);
        AssertNotification(fixture, failure, TestContext.Current.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUME-TIMEOUT", "generation-completes-before-notification")]
    public async Task FaultGeneration_MustCompleteBeforeReceiveNotificationBeginsAsync()
    {
        var fixture = new Fixture(CancellationToken.None, CancellationToken.None, CancellationToken.None);
        var generation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Context.FaultGenerationTask = generation.Task;

        Task notification = fixture.Context.NotifyFaultedAsync(
            fixture.MessageContext,
            Duration,
            ConsumerType,
            new InvalidOperationException("consumer failed"),
            TestContext.Current.CancellationToken);

        Assert.False(notification.IsCompleted);
        Assert.Empty(fixture.Receive.Notifications);
        generation.TrySetResult();
        await notification;
        Assert.Single(fixture.Receive.Notifications);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUME-TIMEOUT", "generation-failure-is-preserved-and-skips-notification")]
    public async Task FaultGenerationFailure_PreservesTheExactFailureAndSkipsReceiveAsync()
    {
        var fixture = new Fixture(CancellationToken.None, CancellationToken.None, CancellationToken.None);
        var failure = new ExpectedFailure("generation failed");
        fixture.Context.FaultGenerationTask = Task.FromException(failure);

        ExpectedFailure actual = await Assert.ThrowsAsync<ExpectedFailure>(() =>
            fixture.Context.NotifyFaultedAsync(
                fixture.MessageContext,
                Duration,
                ConsumerType,
                new InvalidOperationException("consumer failed"),
                TestContext.Current.CancellationToken));

        Assert.Same(failure, actual);
        Assert.Empty(fixture.Receive.Notifications);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUME-TIMEOUT", "generation-cancellation-is-preserved-and-skips-notification")]
    public async Task FaultGenerationCancellation_PreservesTheExactTokenAndSkipsReceiveAsync()
    {
        using var generation = new CancellationTokenSource();
        generation.Cancel();
        var fixture = new Fixture(CancellationToken.None, CancellationToken.None, CancellationToken.None);
        fixture.Context.FaultGenerationTask = Task.FromCanceled(generation.Token);

        OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            fixture.Context.NotifyFaultedAsync(
                fixture.MessageContext,
                Duration,
                ConsumerType,
                new InvalidOperationException("consumer failed"),
                TestContext.Current.CancellationToken));

        Assert.Equal(generation.Token, actual.CancellationToken);
        Assert.Empty(fixture.Receive.Notifications);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUME-TIMEOUT", "late-caller-cancellation-does-not-replace-generation-failure")]
    public async Task CallerCancellationAfterGenerationStarts_DoesNotReplaceTheGenerationFailureAsync()
    {
        using var caller = new CancellationTokenSource();
        var fixture = new Fixture(CancellationToken.None, CancellationToken.None, CancellationToken.None);
        var generation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var failure = new ExpectedFailure("late generation failure");
        fixture.Context.FaultGenerationTask = generation.Task;

        Task notification = fixture.Context.NotifyFaultedAsync(
            fixture.MessageContext, Duration, ConsumerType, new InvalidOperationException("consumer failed"), caller.Token);
        caller.Cancel();
        generation.TrySetException(failure);

        ExpectedFailure actual = await Assert.ThrowsAsync<ExpectedFailure>(() => notification);
        Assert.Same(failure, actual);
        Assert.Empty(fixture.Receive.Notifications);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUME-TIMEOUT", "late-caller-cancellation-does-not-replace-generation-cancellation")]
    public async Task CallerCancellationAfterGenerationStarts_DoesNotReplaceTheGenerationCancellationAsync()
    {
        using var caller = new CancellationTokenSource();
        using var generationCancellation = new CancellationTokenSource();
        var fixture = new Fixture(CancellationToken.None, CancellationToken.None, CancellationToken.None);
        var generation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Context.FaultGenerationTask = generation.Task;

        Task notification = fixture.Context.NotifyFaultedAsync(
            fixture.MessageContext, Duration, ConsumerType, new InvalidOperationException("consumer failed"), caller.Token);
        caller.Cancel();
        generationCancellation.Cancel();
        generation.TrySetCanceled(generationCancellation.Token);

        OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => notification);
        Assert.Equal(generationCancellation.Token, actual.CancellationToken);
        Assert.Empty(fixture.Receive.Notifications);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUME-TIMEOUT", "late-caller-cancellation-does-not-skip-receive")]
    public async Task CallerCancellationAfterGenerationStarts_DoesNotSkipReceiveNotificationAsync()
    {
        using var caller = new CancellationTokenSource();
        var fixture = new Fixture(CancellationToken.None, CancellationToken.None, CancellationToken.None);
        var generation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Context.FaultGenerationTask = generation.Task;

        Task notification = fixture.Context.NotifyFaultedAsync(
            fixture.MessageContext, Duration, ConsumerType, new InvalidOperationException("consumer failed"), caller.Token);
        caller.Cancel();
        generation.TrySetResult();
        await notification;

        FaultNotification received = Assert.Single(fixture.Receive.Notifications);
        Assert.Equal(caller.Token, received.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUME-TIMEOUT", "receive-failure-is-preserved")]
    public async Task ReceiveNotificationFailure_PreservesTheExactFailureAsync()
    {
        var fixture = new Fixture(CancellationToken.None, CancellationToken.None, CancellationToken.None);
        var failure = new ExpectedFailure("receive notification failed");
        fixture.Receive.NotificationTask = Task.FromException(failure);

        ExpectedFailure actual = await Assert.ThrowsAsync<ExpectedFailure>(() =>
            fixture.Context.NotifyFaultedAsync(
                fixture.MessageContext,
                Duration,
                ConsumerType,
                new InvalidOperationException("consumer failed"),
                TestContext.Current.CancellationToken));

        Assert.Same(failure, actual);
        Assert.Single(fixture.Context.GeneratedFaults);
        Assert.Single(fixture.Receive.Notifications);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUME-TIMEOUT", "receive-cancellation-is-preserved")]
    public async Task ReceiveNotificationCancellation_PreservesTheExactTokenAsync()
    {
        using var receiveCancellation = new CancellationTokenSource();
        receiveCancellation.Cancel();
        var fixture = new Fixture(CancellationToken.None, CancellationToken.None, CancellationToken.None);
        fixture.Receive.NotificationTask = Task.FromCanceled(receiveCancellation.Token);

        OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            fixture.Context.NotifyFaultedAsync(
                fixture.MessageContext,
                Duration,
                ConsumerType,
                new InvalidOperationException("consumer failed"),
                TestContext.Current.CancellationToken));

        Assert.Equal(receiveCancellation.Token, actual.CancellationToken);
        Assert.Single(fixture.Context.GeneratedFaults);
        Assert.Single(fixture.Receive.Notifications);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUME-TIMEOUT", "late-caller-cancellation-does-not-replace-receive-failure")]
    public async Task CallerCancellationAfterReceiveStarts_DoesNotReplaceTheReceiveFailureAsync()
    {
        using var caller = new CancellationTokenSource();
        var fixture = new Fixture(CancellationToken.None, CancellationToken.None, CancellationToken.None);
        var receive = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var failure = new ExpectedFailure("late receive failure");
        fixture.Receive.NotificationTask = receive.Task;

        Task notification = fixture.Context.NotifyFaultedAsync(
            fixture.MessageContext, Duration, ConsumerType, new InvalidOperationException("consumer failed"), caller.Token);
        Assert.Single(fixture.Receive.Notifications);
        caller.Cancel();
        receive.TrySetException(failure);

        ExpectedFailure actual = await Assert.ThrowsAsync<ExpectedFailure>(() => notification);
        Assert.Same(failure, actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUME-TIMEOUT", "null-generation-task-is-rejected-before-notification")]
    public async Task MissingFaultGenerationTask_IsRejectedBeforeReceiveNotificationAsync()
    {
        var fixture = new Fixture(CancellationToken.None, CancellationToken.None, CancellationToken.None);
        fixture.Context.FaultGenerationTask = null;

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Context.NotifyFaultedAsync(
                fixture.MessageContext,
                Duration,
                ConsumerType,
                new InvalidOperationException("consumer failed"),
                TestContext.Current.CancellationToken));

        Assert.Equal("The consume context returned no fault-generation task.", exception.Message);
        Assert.Empty(fixture.Receive.Notifications);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUME-TIMEOUT", "null-receive-task-is-rejected-after-generation")]
    public async Task MissingReceiveNotificationTask_IsRejectedAfterFaultGenerationAsync()
    {
        var fixture = new Fixture(CancellationToken.None, CancellationToken.None, CancellationToken.None);
        fixture.Receive.NotificationTask = null;

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Context.NotifyFaultedAsync(
                fixture.MessageContext,
                Duration,
                ConsumerType,
                new InvalidOperationException("consumer failed"),
                TestContext.Current.CancellationToken));

        Assert.Equal("The receive context returned no consume-fault notification task.", exception.Message);
        Assert.Single(fixture.Context.GeneratedFaults);
        Assert.Single(fixture.Receive.Notifications);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUME-TIMEOUT", "pre-canceled-caller-has-no-fault-side-effects")]
    public async Task CanceledCaller_ReturnsTheExactCancellationWithoutGeneratingOrNotifyingAsync()
    {
        using var caller = new CancellationTokenSource();
        caller.Cancel();
        var fixture = new Fixture(CancellationToken.None, CancellationToken.None, CancellationToken.None);

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            fixture.Context.NotifyFaultedAsync(
                fixture.MessageContext,
                Duration,
                ConsumerType,
                new InvalidOperationException("must not be observed"),
                caller.Token));

        Assert.Equal(caller.Token, exception.CancellationToken);
        Assert.Empty(fixture.Context.GeneratedFaults);
        Assert.Empty(fixture.Receive.Notifications);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUME-TIMEOUT", "required-arguments-are-validated-before-cancellation")]
    public void InvalidArguments_AreRejectedSynchronouslyBeforeCancellationWithoutSideEffects()
    {
        using var caller = new CancellationTokenSource();
        caller.Cancel();
        var fixture = new Fixture(CancellationToken.None, CancellationToken.None, CancellationToken.None);
        var failure = new InvalidOperationException("consumer failed");

        Assert.Equal("context", Assert.Throws<ArgumentNullException>((Action)(() =>
            _ = fixture.Context.NotifyFaultedAsync<ProbeMessage>(null!, Duration, ConsumerType, failure, caller.Token))).ParamName);
        Assert.Equal("consumerType", Assert.Throws<ArgumentException>((Action)(() =>
            _ = fixture.Context.NotifyFaultedAsync(fixture.MessageContext, Duration, " ", failure, caller.Token))).ParamName);
        Assert.Equal("exception", Assert.Throws<ArgumentNullException>((Action)(() =>
            _ = fixture.Context.NotifyFaultedAsync(fixture.MessageContext, Duration, ConsumerType, null!, caller.Token))).ParamName);
        Assert.Empty(fixture.Context.GeneratedFaults);
        Assert.Empty(fixture.Receive.Notifications);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUME-TIMEOUT", "typed-message-and-consumed-convenience-forwarding")]
    public async Task MessageAndConsumedNotification_ForwardTheTimeoutContextIdentityAsync()
    {
        using var caller = new CancellationTokenSource();
        var fixture = new Fixture(CancellationToken.None, CancellationToken.None, CancellationToken.None);

        Assert.Same(fixture.OwnerMessage, fixture.Context.Message);
        await fixture.Context.NotifyConsumedAsync(Duration, ConsumerType, caller.Token);

        ConsumedNotification notification = Assert.Single(fixture.Owner.ConsumedNotifications);
        Assert.Same(fixture.Context, notification.Context);
        Assert.Equal(Duration, notification.Duration);
        Assert.Equal(ConsumerType, notification.ConsumerType);
        Assert.Equal(caller.Token, notification.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUME-TIMEOUT", "faulted-convenience-overload-uses-timeout-context")]
    public async Task FaultedConvenienceOverload_UsesTheTimeoutContextAsTheMessageContextAsync()
    {
        var fixture = new Fixture(CancellationToken.None, CancellationToken.None, CancellationToken.None);
        var failure = new InvalidOperationException("consumer failed");

        await fixture.Context.NotifyFaultedAsync(
            Duration,
            ConsumerType,
            failure,
            TestContext.Current.CancellationToken);

        Assert.Same(fixture.OwnerContext, Assert.Single(fixture.Context.GeneratedFaults).Context);
        FaultNotification notification = Assert.Single(fixture.Receive.Notifications);
        Assert.Same(fixture.Context, notification.Context);
        Assert.Same(failure, notification.Exception);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUME-TIMEOUT", "faulted-convenience-overload-reclassifies-own-timeout")]
    public async Task TimeoutThroughFaultedConvenienceOverload_IsReclassifiedAndGeneratedAsync()
    {
        using var timeout = new CancellationTokenSource();
        timeout.Cancel();
        var fixture = new Fixture(CancellationToken.None, CancellationToken.None, timeout.Token);
        var cancellation = new OperationCanceledException("consume timed out", null, timeout.Token);

        await fixture.Context.NotifyFaultedAsync(
            Duration,
            ConsumerType,
            cancellation,
            TestContext.Current.CancellationToken);

        GeneratedFault generated = Assert.Single(fixture.Context.GeneratedFaults);
        Assert.Same(fixture.OwnerContext, generated.Context);
        ConsumerCanceledException reported = Assert.IsType<ConsumerCanceledException>(generated.Exception);
        Assert.Same(cancellation, reported.InnerException);
        Assert.Same(reported, Assert.Single(fixture.Receive.Notifications).Exception);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUME-TIMEOUT", "scoped-convenience-overload-retains-timeout-authority")]
    public async Task TimeoutThroughScopedConvenienceOverload_IsReclassifiedForTheScopeAsync()
    {
        using var timeout = new CancellationTokenSource();
        timeout.Cancel();
        var fixture = new Fixture(CancellationToken.None, CancellationToken.None, timeout.Token);
        var scope = new ConsumeContextScope<ProbeMessage>(fixture.Context);
        var cancellation = new OperationCanceledException("scoped consume timed out", null, timeout.Token);

        await scope.NotifyFaultedAsync(
            Duration,
            ConsumerType,
            cancellation,
            TestContext.Current.CancellationToken);

        GeneratedFault generated = Assert.Single(fixture.Context.GeneratedFaults);
        Assert.Same(fixture.OwnerContext, generated.Context);
        ConsumerCanceledException reported = Assert.IsType<ConsumerCanceledException>(generated.Exception);
        Assert.Same(cancellation, reported.InnerException);
        Assert.Same(scope, Assert.Single(fixture.Receive.Notifications).Context);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUME-TIMEOUT", "foreign-timeout-payload-does-not-borrow-owner-authority")]
    public async Task ScopeAroundAnotherTimeoutContext_UsesThePassedContextCancellationAsync()
    {
        using var stoppedOwner = new CancellationTokenSource();
        stoppedOwner.Cancel();
        var fixture = new Fixture(stoppedOwner.Token, CancellationToken.None, CancellationToken.None);
        var foreign = new Fixture(CancellationToken.None, CancellationToken.None, CancellationToken.None);
        var foreignScope = new ConsumeContextScope<ProbeMessage>(foreign.Context);
        var failure = new InvalidOperationException("foreign timeout context remains active");

        await fixture.Context.NotifyFaultedAsync(
            foreignScope,
            Duration,
            ConsumerType,
            failure,
            TestContext.Current.CancellationToken);

        GeneratedFault generated = Assert.Single(fixture.Context.GeneratedFaults);
        Assert.Same(foreignScope, generated.Context);
        Assert.Same(failure, generated.Exception);
        Assert.Same(foreignScope, Assert.Single(fixture.Receive.Notifications).Context);
        Assert.Empty(foreign.Context.GeneratedFaults);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [RequirementCoverage("REQ-VSB-CONSUME-TIMEOUT", "timeout-must-be-positive")]
    public void Constructor_RejectsNonpositiveTimeout(int milliseconds)
    {
        var fixture = new FixtureSource(CancellationToken.None);

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RecordingTimeoutConsumeContext(
                fixture.OwnerContext,
                CancellationToken.None,
                TimeSpan.FromMilliseconds(milliseconds)));

        Assert.Equal("timeout", exception.ParamName);
        Assert.Equal(TimeSpan.FromMilliseconds(milliseconds), exception.ActualValue);
    }

    static void AssertNotification(Fixture fixture, Exception expectedException, CancellationToken expectedCancellationToken)
    {
        FaultNotification notification = Assert.Single(fixture.Receive.Notifications);
        Assert.Same(fixture.MessageContext, notification.Context);
        Assert.Equal(Duration, notification.Duration);
        Assert.Equal(ConsumerType, notification.ConsumerType);
        Assert.Same(expectedException, notification.Exception);
        Assert.Equal(expectedCancellationToken, notification.CancellationToken);
    }

    sealed class Fixture
    {
        public Fixture(CancellationToken ownerToken, CancellationToken messageToken, CancellationToken timeoutToken)
        {
            var source = new FixtureSource(ownerToken);
            Owner = source.Owner;
            OwnerContext = source.OwnerContext;
            OwnerMessage = source.OwnerMessage;
            Receive = source.Receive;
            Context = new RecordingTimeoutConsumeContext(source.OwnerContext, timeoutToken, ConfiguredTimeout);
            MessageContext = DispatchProxy.Create<ConsumeContext<ProbeMessage>, MessageContextState>();
            ((MessageContextState)(object)MessageContext).CancellationToken = messageToken;
        }

        public RecordingTimeoutConsumeContext Context { get; }
        public ConsumeContext<ProbeMessage> MessageContext { get; }
        public ConsumeContext<ProbeMessage> OwnerContext { get; }
        public ProbeMessage OwnerMessage { get; }
        public OwnerContextState Owner { get; }
        public RecordingReceiveContextProxy Receive { get; }
    }

    sealed class FixtureSource
    {
        public FixtureSource(CancellationToken ownerToken)
        {
            ReceiveContext receiveContext = DispatchProxy.Create<ReceiveContext, RecordingReceiveContextProxy>();
            Receive = (RecordingReceiveContextProxy)(object)receiveContext;
            ConsumeContext ownerContext = DispatchProxy.Create<ConsumeContext, OwnerContextState>();
            Owner = (OwnerContextState)(object)ownerContext;
            OwnerMessage = new ProbeMessage();
            Owner.CancellationToken = ownerToken;
            Owner.ReceiveContext = receiveContext;
            Owner.SerializerContext = DispatchProxy.Create<SerializerContext, UnexpectedInvocationProxy>();
            OwnerContext = new MessageConsumeContext<ProbeMessage>(ownerContext, OwnerMessage);
        }

        public ConsumeContext<ProbeMessage> OwnerContext { get; }
        public ProbeMessage OwnerMessage { get; }
        public OwnerContextState Owner { get; }
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

    sealed record ConsumedNotification(
        object Context,
        TimeSpan Duration,
        string ConsumerType,
        CancellationToken CancellationToken);

    sealed class ExpectedFailure(string message) : Exception(message);

    sealed class RecordingTimeoutConsumeContext(
        ConsumeContext<ProbeMessage> context,
        CancellationToken cancellationToken,
        TimeSpan timeout)
        : TimeoutConsumeContext<ProbeMessage>(context, cancellationToken, timeout)
    {
        public Task? FaultGenerationTask { get; set; } = Task.CompletedTask;

        public List<GeneratedFault> GeneratedFaults { get; } = [];

        protected override Task GenerateFaultAsync<T>(ConsumeContext<T> context, Exception exception)
        {
            GeneratedFaults.Add(new GeneratedFault(context, exception));
            return FaultGenerationTask!;
        }
    }

    class OwnerContextState : DispatchProxy
    {
        public CancellationToken CancellationToken { get; set; }

        public ReceiveContext ReceiveContext { get; set; } = null!;

        public SerializerContext SerializerContext { get; set; } = null!;

        public List<ConsumedNotification> ConsumedNotifications { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            MethodInfo method = targetMethod ?? throw new InvalidOperationException("The owner context supplied no method metadata.");
            if (method.Name == "get_CancellationToken")
                return CancellationToken;
            if (method.Name == "get_ReceiveContext")
                return ReceiveContext;
            if (method.Name == "get_SerializerContext")
                return SerializerContext;
            if (method.Name == nameof(ConsumeContext.NotifyConsumedAsync) && args?.Length == 4)
            {
                ConsumedNotifications.Add(new ConsumedNotification(
                    args[0]!,
                    (TimeSpan)args[1]!,
                    (string)args[2]!,
                    (CancellationToken)args[3]!));
                return Task.CompletedTask;
            }

            throw new InvalidOperationException($"The owner context unexpectedly invoked {method.Name}.");
        }
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

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            MethodInfo method = targetMethod ?? throw new InvalidOperationException("The message context supplied no method metadata.");
            if (method.Name == "get_CancellationToken")
                return CancellationToken;
            if (method.Name == nameof(PipeContext.TryGetPayload))
                return false;

            throw new InvalidOperationException($"The message context unexpectedly invoked {method.Name}.");
        }
    }

    class UnexpectedInvocationProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"The timeout consume test unexpectedly invoked {targetMethod?.Name}.");
    }
}
