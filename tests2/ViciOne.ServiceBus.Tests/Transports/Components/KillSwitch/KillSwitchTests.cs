using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.KillSwitch;
using ViciOne.ServiceBus.Tests.Testing;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transports.Components.KillSwitch;

public sealed class KillSwitchTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-KILL-SWITCH-TRIP", "exact-activation-and-ratio-boundary")]
    public async Task ExactActivationAndRatioBoundary_TripsExactlyOnce()
    {
        (KillSwitchTestDriver driver, _) = CreateDriver(activationThreshold: 4, tripThresholdRatio: 0.50);

        await driver.ObserveSuccess();
        await driver.ObserveSuccess();
        await driver.ObserveFailure(new InvalidOperationException("first matching failure"));
        Assert.Equal(0, driver.PauseCount);

        await driver.ObserveFailure(new InvalidOperationException("exact boundary failure"));
        await WaitForPause(driver, 1);

        Assert.Equal(1, driver.PauseCount);
        Assert.Equal(KillSwitchTestState.Paused, driver.Snapshot.State);
        await CancelRecovery(driver);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-KILL-SWITCH-TRIP", "below-activation-does-not-trip")]
    public async Task MatchingRatioBelowActivationThreshold_DoesNotTrip()
    {
        (KillSwitchTestDriver driver, _) = CreateDriver(activationThreshold: 3, tripThresholdRatio: 0.50);

        await driver.ObserveFailure(new InvalidOperationException("failure one"));
        await driver.ObserveFailure(new InvalidOperationException("failure two"));

        Assert.Equal(0, driver.PauseCount);
        Assert.Equal(KillSwitchTestState.Running, driver.Snapshot.State);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-KILL-SWITCH-TRIP", "below-ratio-does-not-trip")]
    public async Task FailureRatioBelowConfiguredBoundary_DoesNotTrip()
    {
        (KillSwitchTestDriver driver, _) = CreateDriver(activationThreshold: 4, tripThresholdRatio: 0.75);

        await driver.ObserveSuccess();
        await driver.ObserveSuccess();
        await driver.ObserveFailure(new InvalidOperationException("failure one"));
        await driver.ObserveFailure(new InvalidOperationException("failure two"));

        Assert.Equal(0, driver.PauseCount);
        Assert.Equal(
            new KillSwitchTestSnapshot(KillSwitchTestState.Running, 4, 2, 2, false),
            driver.Snapshot);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-KILL-SWITCH-TRIP", "ignored-exception-does-not-contribute")]
    public async Task ExceptionOutsideConfiguredFilter_DoesNotContributeToFailureRatio()
    {
        var options = new KillSwitchOptions()
            .SetActivationThreshold(1)
            .SetTripThresholdRatio(0)
            .SetExceptionFilter(filter => filter.Handle<InvalidOperationException>());
        KillSwitchTestDriver driver = CreateDriver(options);

        await driver.ObserveFailure(new ArgumentException("excluded"));

        Assert.Equal(0, driver.PauseCount);
        Assert.Equal(0, driver.Snapshot.FailureCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-KILL-SWITCH-TRIP", "lazy-window-reset")]
    public async Task FirstObservationAfterTrackingPeriod_StartsAFreshWindowWithoutATimer()
    {
        (KillSwitchTestDriver driver, ObservableTimeProvider time) = CreateDriver(
            activationThreshold: 2,
            tripThresholdRatio: 1,
            trackingPeriod: TimeSpan.FromMinutes(1));
        await driver.ObserveFailure(new InvalidOperationException("old window"));

        time.Advance(TimeSpan.FromMinutes(1));
        await driver.ObserveFailure(new InvalidOperationException("new window"));

        Assert.Equal(0, driver.PauseCount);
        Assert.Equal(
            new KillSwitchTestSnapshot(KillSwitchTestState.Running, 1, 0, 1, false),
            driver.Snapshot);
        Assert.Equal(0, time.TimerCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-KILL-SWITCH-RECOVERY", "single-owner-under-concurrency")]
    public async Task ConcurrentThresholdFailures_CreateOneRecoveryOwner()
    {
        const int concurrency = 32;
        (KillSwitchTestDriver driver, _) = CreateDriver(activationThreshold: 1, tripThresholdRatio: 0);
        for (var index = 0; index < concurrency; index++)
            await driver.ObserveAttempt();

        // Task.Run is intentional here: this test attacks the product's synchronization boundary.
        await Task.WhenAll(Enumerable.Range(0, concurrency).Select(index => Task.Run(() =>
            driver.ObserveMatchingFailure(new InvalidOperationException($"failure {index}")))));
        await WaitForPause(driver, 1);

        Assert.Equal(1, driver.PauseCount);
        Assert.True(driver.Snapshot.RecoveryActive);
        await CancelRecovery(driver);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-KILL-SWITCH-RECOVERY", "pause-delay-start-order")]
    public async Task Recovery_PausesWaitsAndThenRestartsInOrder()
    {
        (KillSwitchTestDriver driver, ObservableTimeProvider time) = CreateDriver(
            activationThreshold: 1,
            tripThresholdRatio: 0,
            restartDelay: TimeSpan.FromSeconds(3));

        await driver.ObserveFailure(new InvalidOperationException("trip"));
        await WaitForPause(driver, 1);
        await WaitForTimer(time, 1);
        Assert.Equal(["pause"], driver.Events);
        Assert.Equal(KillSwitchTestState.Paused, driver.Snapshot.State);

        time.Advance(TimeSpan.FromSeconds(3) - TimeSpan.FromTicks(1));
        Assert.Equal(0, driver.StartCount);
        time.Advance(TimeSpan.FromTicks(1));
        await WaitForStart(driver, 1);
        await driver.RecoveryTask.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);

        Assert.Equal(["pause", "start"], driver.Events);
        Assert.Equal(
            new KillSwitchTestSnapshot(KillSwitchTestState.VerifyingRecovery, 0, 0, 0, false),
            driver.Snapshot);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-KILL-SWITCH-RECOVERY", "matching-failure-during-verification-retrips-immediately")]
    public async Task MatchingFailureDuringRecoveryVerification_RetripsImmediately()
    {
        (KillSwitchTestDriver driver, ObservableTimeProvider time) = CreateDriver(
            activationThreshold: 4,
            tripThresholdRatio: 1);

        for (var index = 0; index < 4; index++)
            await driver.ObserveFailure(new InvalidOperationException($"initial failure {index}"));

        await WaitForPause(driver, 1);
        await WaitForTimer(time, 1);
        time.Advance(TimeSpan.FromSeconds(1));
        await WaitForStart(driver, 1);
        await driver.RecoveryTask.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);
        Assert.Equal(KillSwitchTestState.VerifyingRecovery, driver.Snapshot.State);

        await driver.ObserveFailure(new InvalidOperationException("recovery verification failed"));
        await WaitForPause(driver, 2);

        Assert.Equal(2, driver.PauseCount);
        Assert.Equal(KillSwitchTestState.Paused, driver.Snapshot.State);
        await CancelRecovery(driver);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-KILL-SWITCH-RECOVERY", "exact-success-boundary-completes-verification")]
    public async Task SuccessfulDeliveriesDuringRecoveryVerification_ReturnToRunningAtExactActivationBoundary()
    {
        (KillSwitchTestDriver driver, ObservableTimeProvider time) = CreateDriver(
            activationThreshold: 3,
            tripThresholdRatio: 1);

        for (var index = 0; index < 3; index++)
            await driver.ObserveFailure(new InvalidOperationException($"initial failure {index}"));

        await WaitForPause(driver, 1);
        await WaitForTimer(time, 1);
        time.Advance(TimeSpan.FromSeconds(1));
        await WaitForStart(driver, 1);
        await driver.RecoveryTask.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);

        await driver.ObserveSuccess();
        await driver.ObserveSuccess();
        Assert.Equal(
            new KillSwitchTestSnapshot(KillSwitchTestState.VerifyingRecovery, 2, 2, 0, false),
            driver.Snapshot);

        await driver.ObserveSuccess();

        Assert.Equal(
            new KillSwitchTestSnapshot(KillSwitchTestState.Running, 0, 0, 0, false),
            driver.Snapshot);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-KILL-SWITCH-RECOVERY", "pause-failure-bounded-retry")]
    public async Task PauseFailure_RetriesOnlyAfterTheConfiguredDelay()
    {
        (KillSwitchTestDriver driver, ObservableTimeProvider time) = CreateDriver(
            activationThreshold: 1,
            tripThresholdRatio: 0);
        driver.EnqueuePauseFailure(new InvalidOperationException("pause failed"));

        await driver.ObserveFailure(new InvalidOperationException("trip"));
        await WaitForPause(driver, 1);
        await WaitForTimer(time, 1);
        Assert.Equal(1, driver.PauseCount);

        time.Advance(TimeSpan.FromSeconds(1));
        await WaitForPause(driver, 2);
        await WaitForTimer(time, 2);

        Assert.Equal(["pause", "pause"], driver.Events);
        Assert.Equal(KillSwitchTestState.Paused, driver.Snapshot.State);
        await CancelRecovery(driver);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-KILL-SWITCH-RECOVERY", "restart-failure-cleanup-and-retry")]
    public async Task RestartFailure_IsPausedAgainAndRetriedWithoutStrandingTheSwitch()
    {
        (KillSwitchTestDriver driver, ObservableTimeProvider time) = CreateDriver(
            activationThreshold: 1,
            tripThresholdRatio: 0);
        driver.EnqueueStartFailure(new InvalidOperationException("start failed"));

        await driver.ObserveFailure(new InvalidOperationException("trip"));
        await WaitForPause(driver, 1);
        await WaitForTimer(time, 1);
        time.Advance(TimeSpan.FromSeconds(1));
        await WaitForStart(driver, 1);
        await WaitForPause(driver, 2);
        await WaitForTimer(time, 2);

        time.Advance(TimeSpan.FromSeconds(1));
        await WaitForStart(driver, 2);
        await driver.RecoveryTask.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);

        Assert.Equal(["pause", "start", "pause", "start"], driver.Events);
        Assert.Equal(KillSwitchTestState.VerifyingRecovery, driver.Snapshot.State);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-KILL-SWITCH-RECOVERY", "terminal-stop-cancels-restart")]
    public async Task TerminalEndpointStop_CancelsAPendingRestartAndPreventsLateStart()
    {
        (KillSwitchTestDriver driver, ObservableTimeProvider time) = CreateDriver(
            activationThreshold: 1,
            tripThresholdRatio: 0);

        await driver.ObserveFailure(new InvalidOperationException("trip"));
        await WaitForPause(driver, 1);
        await WaitForTimer(time, 1);
        await driver.StopEndpoint().WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);

        time.Advance(TimeSpan.FromDays(1));

        Assert.Equal(0, driver.StartCount);
        Assert.Equal(KillSwitchTestState.Terminated, driver.Snapshot.State);
        Assert.False(driver.Snapshot.RecoveryActive);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-KILL-SWITCH-RECOVERY", "external-restart-rearms-lifecycle")]
    public async Task LaterExternalReady_RearmsATerminatedSwitchWithoutDuplicatingTheObserver()
    {
        (KillSwitchTestDriver driver, _) = CreateDriver(activationThreshold: 1, tripThresholdRatio: 0);
        await driver.StopEndpoint();

        await driver.Rearm();
        await driver.ObserveFailure(new InvalidOperationException("trip after external restart"));
        await WaitForPause(driver, 1);

        Assert.Equal(1, driver.ConnectCount);
        Assert.Equal(1, driver.PauseCount);
        await CancelRecovery(driver);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-KILL-SWITCH-RECOVERY", "endpoint-identity-boundary")]
    public async Task OneSwitchInstance_RejectsASecondEndpointIdentity()
    {
        (KillSwitchTestDriver driver, ObservableTimeProvider time) = CreateDriver();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(driver.AttachDifferentEndpoint);

        Assert.Contains("cannot observe more than one", exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, time.TimerCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-KILL-SWITCH-OBSERVABILITY", "owned-log-context")]
    public async Task Recovery_RestoresTheEndpointLogContextBeforePauseAndRestart()
    {
        var ownContext = new BusLogContext(NullLoggerFactory.Instance);
        var foreignContext = new BusLogContext(NullLoggerFactory.Instance);
        (KillSwitchTestDriver driver, ObservableTimeProvider time) = CreateDriver(
            activationThreshold: 1,
            tripThresholdRatio: 0,
            logContext: ownContext);
        LogContext.Current = foreignContext;

        await driver.ObserveFailure(new InvalidOperationException("trip"));
        await WaitForPause(driver, 1);
        await WaitForTimer(time, 1);
        time.Advance(TimeSpan.FromSeconds(1));
        await WaitForStart(driver, 1);
        await driver.RecoveryTask.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);

        Assert.Same(ownContext, driver.LogContextSeenByLastPause);
        Assert.Same(ownContext, driver.LogContextSeenByLastRestart);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-KILL-SWITCH-OBSERVABILITY", "caller-log-context-preserved")]
    public async Task ThresholdLogging_DoesNotReplaceTheCallersAmbientLogContext()
    {
        var ownContext = new BusLogContext(NullLoggerFactory.Instance);
        var callerContext = new BusLogContext(NullLoggerFactory.Instance);
        (KillSwitchTestDriver driver, _) = CreateDriver(
            activationThreshold: 1,
            tripThresholdRatio: 0,
            logContext: ownContext);
        LogContext.Current = callerContext;

        await driver.ObserveAttempt();
        Task observation = driver.ObserveMatchingFailure(new InvalidOperationException("trip"));

        Assert.True(observation.IsCompletedSuccessfully);
        Assert.Same(callerContext, LogContext.Current);
        await WaitForPause(driver, 1);
        await CancelRecovery(driver);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-KILL-SWITCH-OBSERVATION", "consumer-and-routing-slip-parity")]
    public async Task ConsumerAndRoutingSlipCallbacks_ContributeToTheSameCounters()
    {
        (KillSwitchTestDriver driver, _) = CreateDriver(activationThreshold: 10, tripThresholdRatio: 1);

        await driver.ObserveConsumerAndRoutingSlipCallbacks(new InvalidOperationException("execute"));

        Assert.Equal(
            new KillSwitchTestSnapshot(KillSwitchTestState.Running, 3, 2, 1, false),
            driver.Snapshot);
    }

    private static (KillSwitchTestDriver Driver, ObservableTimeProvider Time) CreateDriver(
        int activationThreshold = 5,
        double tripThresholdRatio = 0.50,
        TimeSpan? trackingPeriod = null,
        TimeSpan? restartDelay = null,
        ILogContext? logContext = null)
    {
        var time = new ObservableTimeProvider(new DateTimeOffset(2035, 1, 2, 3, 4, 5, TimeSpan.Zero));
        var options = new KillSwitchOptions()
            .SetActivationThreshold(activationThreshold)
            .SetTripThresholdRatio(tripThresholdRatio)
            .SetTrackingPeriod(trackingPeriod ?? TimeSpan.FromMinutes(1))
            .SetRestartDelay(restartDelay ?? TimeSpan.FromSeconds(1))
            .SetTimeProvider(time);
        return (CreateDriver(options, logContext), time);
    }

    private static KillSwitchTestDriver CreateDriver(KillSwitchOptions options, ILogContext? logContext = null) =>
        new(options, logContext ?? new BusLogContext(NullLoggerFactory.Instance));

    private static Task WaitForPause(KillSwitchTestDriver driver, int count) =>
        driver.WaitForPauseCount(count, OperationTimeout(), TestContext.Current.CancellationToken);

    private static Task WaitForStart(KillSwitchTestDriver driver, int count) =>
        driver.WaitForStartCount(count, OperationTimeout(), TestContext.Current.CancellationToken);

    private static Task WaitForTimer(ObservableTimeProvider time, int count) =>
        time.WaitForTimerCount(count).WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);

    private static Task CancelRecovery(KillSwitchTestDriver driver) =>
        driver.StopEndpoint().WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);

    private static TimeSpan OperationTimeout() =>
        TestConfigurationProvider.ForCurrentTestRun().GetValidatedOptions().OperationTimeout!.Value;
}
