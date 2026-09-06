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
    public async Task ExactActivationAndRatioBoundary_TripsExactlyOnceAsync()
    {
        (KillSwitchTestDriver driver, _) = CreateDriver(activationThreshold: 4, tripThresholdRatio: 0.50);

        await driver.ObserveSuccessAsync();
        await driver.ObserveSuccessAsync();
        await driver.ObserveFailureAsync(new InvalidOperationException("first matching failure"));
        Assert.Equal(0, driver.PauseCount);

        await driver.ObserveFailureAsync(new InvalidOperationException("exact boundary failure"));
        await WaitForPauseAsync(driver, 1);

        Assert.Equal(1, driver.PauseCount);
        Assert.Equal(KillSwitchTestState.Paused, driver.Snapshot.State);
        await CancelRecoveryAsync(driver);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-KILL-SWITCH-TRIP", "below-activation-does-not-trip")]
    public async Task MatchingRatioBelowActivationThreshold_DoesNotTripAsync()
    {
        (KillSwitchTestDriver driver, _) = CreateDriver(activationThreshold: 3, tripThresholdRatio: 0.50);

        await driver.ObserveFailureAsync(new InvalidOperationException("failure one"));
        await driver.ObserveFailureAsync(new InvalidOperationException("failure two"));

        Assert.Equal(0, driver.PauseCount);
        Assert.Equal(KillSwitchTestState.Running, driver.Snapshot.State);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-KILL-SWITCH-TRIP", "below-ratio-does-not-trip")]
    public async Task FailureRatioBelowConfiguredBoundary_DoesNotTripAsync()
    {
        (KillSwitchTestDriver driver, _) = CreateDriver(activationThreshold: 4, tripThresholdRatio: 0.75);

        await driver.ObserveSuccessAsync();
        await driver.ObserveSuccessAsync();
        await driver.ObserveFailureAsync(new InvalidOperationException("failure one"));
        await driver.ObserveFailureAsync(new InvalidOperationException("failure two"));

        Assert.Equal(0, driver.PauseCount);
        Assert.Equal(
            new KillSwitchTestSnapshot(KillSwitchTestState.Running, 4, 2, 2, false),
            driver.Snapshot);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-KILL-SWITCH-TRIP", "ignored-exception-does-not-contribute")]
    public async Task ExceptionOutsideConfiguredFilter_DoesNotContributeToFailureRatioAsync()
    {
        var options = new KillSwitchOptions()
            .SetActivationThreshold(1)
            .SetTripThresholdRatio(0)
            .SetExceptionFilter(filter => filter.Handle<InvalidOperationException>());
        KillSwitchTestDriver driver = CreateDriver(options);

        await driver.ObserveFailureAsync(new ArgumentException("excluded"));

        Assert.Equal(0, driver.PauseCount);
        Assert.Equal(0, driver.Snapshot.FailureCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-KILL-SWITCH-TRIP", "lazy-window-reset")]
    public async Task FirstObservationAfterTrackingPeriod_StartsAFreshWindowWithoutATimerAsync()
    {
        (KillSwitchTestDriver driver, ObservableTimeProvider time) = CreateDriver(
            activationThreshold: 2,
            tripThresholdRatio: 1,
            trackingPeriod: TimeSpan.FromMinutes(1));
        await driver.ObserveFailureAsync(new InvalidOperationException("old window"));

        time.Advance(TimeSpan.FromMinutes(1));
        await driver.ObserveFailureAsync(new InvalidOperationException("new window"));

        Assert.Equal(0, driver.PauseCount);
        Assert.Equal(
            new KillSwitchTestSnapshot(KillSwitchTestState.Running, 1, 0, 1, false),
            driver.Snapshot);
        Assert.Equal(0, time.TimerCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-KILL-SWITCH-RECOVERY", "single-owner-under-concurrency")]
    public async Task ConcurrentThresholdFailures_CreateOneRecoveryOwnerAsync()
    {
        const int concurrency = 32;
        (KillSwitchTestDriver driver, _) = CreateDriver(activationThreshold: 1, tripThresholdRatio: 0);
        for (var index = 0; index < concurrency; index++)
            await driver.ObserveAttemptAsync();

        // Task.Run is intentional here: this test attacks the product's synchronization boundary.
        await Task.WhenAll(Enumerable.Range(0, concurrency).Select(index => Task.Run(() =>
            driver.ObserveMatchingFailureAsync(new InvalidOperationException($"failure {index}")))));
        await WaitForPauseAsync(driver, 1);

        Assert.Equal(1, driver.PauseCount);
        Assert.True(driver.Snapshot.RecoveryActive);
        await CancelRecoveryAsync(driver);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-KILL-SWITCH-RECOVERY", "pause-delay-start-order")]
    public async Task Recovery_PausesWaitsAndThenRestartsInOrderAsync()
    {
        (KillSwitchTestDriver driver, ObservableTimeProvider time) = CreateDriver(
            activationThreshold: 1,
            tripThresholdRatio: 0,
            restartDelay: TimeSpan.FromSeconds(3));

        await driver.ObserveFailureAsync(new InvalidOperationException("trip"));
        await WaitForPauseAsync(driver, 1);
        await WaitForTimerAsync(time, 1);
        Assert.Equal(["pause"], driver.Events);
        Assert.Equal(KillSwitchTestState.Paused, driver.Snapshot.State);

        time.Advance(TimeSpan.FromSeconds(3) - TimeSpan.FromTicks(1));
        Assert.Equal(0, driver.StartCount);
        time.Advance(TimeSpan.FromTicks(1));
        await WaitForStartAsync(driver, 1);
        await driver.RecoveryTask.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);

        Assert.Equal(["pause", "start"], driver.Events);
        Assert.Equal(
            new KillSwitchTestSnapshot(KillSwitchTestState.VerifyingRecovery, 0, 0, 0, false),
            driver.Snapshot);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-KILL-SWITCH-RECOVERY", "matching-failure-during-verification-retrips-immediately")]
    public async Task MatchingFailureDuringRecoveryVerification_RetripsImmediatelyAsync()
    {
        (KillSwitchTestDriver driver, ObservableTimeProvider time) = CreateDriver(
            activationThreshold: 4,
            tripThresholdRatio: 1);

        for (var index = 0; index < 4; index++)
            await driver.ObserveFailureAsync(new InvalidOperationException($"initial failure {index}"));

        await WaitForPauseAsync(driver, 1);
        await WaitForTimerAsync(time, 1);
        time.Advance(TimeSpan.FromSeconds(1));
        await WaitForStartAsync(driver, 1);
        await driver.RecoveryTask.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);
        Assert.Equal(KillSwitchTestState.VerifyingRecovery, driver.Snapshot.State);

        await driver.ObserveFailureAsync(new InvalidOperationException("recovery verification failed"));
        await WaitForPauseAsync(driver, 2);

        Assert.Equal(2, driver.PauseCount);
        Assert.Equal(KillSwitchTestState.Paused, driver.Snapshot.State);
        await CancelRecoveryAsync(driver);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-KILL-SWITCH-RECOVERY", "exact-success-boundary-completes-verification")]
    public async Task SuccessfulDeliveriesDuringRecoveryVerification_ReturnToRunningAtExactActivationBoundaryAsync()
    {
        (KillSwitchTestDriver driver, ObservableTimeProvider time) = CreateDriver(
            activationThreshold: 3,
            tripThresholdRatio: 1);

        for (var index = 0; index < 3; index++)
            await driver.ObserveFailureAsync(new InvalidOperationException($"initial failure {index}"));

        await WaitForPauseAsync(driver, 1);
        await WaitForTimerAsync(time, 1);
        time.Advance(TimeSpan.FromSeconds(1));
        await WaitForStartAsync(driver, 1);
        await driver.RecoveryTask.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);

        await driver.ObserveSuccessAsync();
        await driver.ObserveSuccessAsync();
        Assert.Equal(
            new KillSwitchTestSnapshot(KillSwitchTestState.VerifyingRecovery, 2, 2, 0, false),
            driver.Snapshot);

        await driver.ObserveSuccessAsync();

        Assert.Equal(
            new KillSwitchTestSnapshot(KillSwitchTestState.Running, 0, 0, 0, false),
            driver.Snapshot);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-KILL-SWITCH-RECOVERY", "pause-failure-bounded-retry")]
    public async Task PauseFailure_RetriesOnlyAfterTheConfiguredDelayAsync()
    {
        (KillSwitchTestDriver driver, ObservableTimeProvider time) = CreateDriver(
            activationThreshold: 1,
            tripThresholdRatio: 0);
        driver.EnqueuePauseFailure(new InvalidOperationException("pause failed"));

        await driver.ObserveFailureAsync(new InvalidOperationException("trip"));
        await WaitForPauseAsync(driver, 1);
        await WaitForTimerAsync(time, 1);
        Assert.Equal(1, driver.PauseCount);

        time.Advance(TimeSpan.FromSeconds(1));
        await WaitForPauseAsync(driver, 2);
        await WaitForTimerAsync(time, 2);

        Assert.Equal(["pause", "pause"], driver.Events);
        Assert.Equal(KillSwitchTestState.Paused, driver.Snapshot.State);
        await CancelRecoveryAsync(driver);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-KILL-SWITCH-RECOVERY", "restart-failure-cleanup-and-retry")]
    public async Task RestartFailure_IsPausedAgainAndRetriedWithoutStrandingTheSwitchAsync()
    {
        (KillSwitchTestDriver driver, ObservableTimeProvider time) = CreateDriver(
            activationThreshold: 1,
            tripThresholdRatio: 0);
        driver.EnqueueStartFailure(new InvalidOperationException("start failed"));

        await driver.ObserveFailureAsync(new InvalidOperationException("trip"));
        await WaitForPauseAsync(driver, 1);
        await WaitForTimerAsync(time, 1);
        time.Advance(TimeSpan.FromSeconds(1));
        await WaitForStartAsync(driver, 1);
        await WaitForPauseAsync(driver, 2);
        await WaitForTimerAsync(time, 2);

        time.Advance(TimeSpan.FromSeconds(1));
        await WaitForStartAsync(driver, 2);
        await driver.RecoveryTask.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);

        Assert.Equal(["pause", "start", "pause", "start"], driver.Events);
        Assert.Equal(KillSwitchTestState.VerifyingRecovery, driver.Snapshot.State);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-KILL-SWITCH-RECOVERY", "terminal-stop-cancels-restart")]
    public async Task TerminalEndpointStop_CancelsAPendingRestartAndPreventsLateStartAsync()
    {
        (KillSwitchTestDriver driver, ObservableTimeProvider time) = CreateDriver(
            activationThreshold: 1,
            tripThresholdRatio: 0);

        await driver.ObserveFailureAsync(new InvalidOperationException("trip"));
        await WaitForPauseAsync(driver, 1);
        await WaitForTimerAsync(time, 1);
        await driver.StopEndpointAsync().WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);

        time.Advance(TimeSpan.FromDays(1));

        Assert.Equal(0, driver.StartCount);
        Assert.Equal(KillSwitchTestState.Terminated, driver.Snapshot.State);
        Assert.False(driver.Snapshot.RecoveryActive);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-KILL-SWITCH-RECOVERY", "external-restart-rearms-lifecycle")]
    public async Task LaterExternalReady_RearmsATerminatedSwitchWithoutDuplicatingTheObserverAsync()
    {
        (KillSwitchTestDriver driver, _) = CreateDriver(activationThreshold: 1, tripThresholdRatio: 0);
        await driver.StopEndpointAsync();

        driver.Rearm();
        await driver.ObserveFailureAsync(new InvalidOperationException("trip after external restart"));
        await WaitForPauseAsync(driver, 1);

        Assert.Equal(1, driver.ConnectCount);
        Assert.Equal(1, driver.PauseCount);
        await CancelRecoveryAsync(driver);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-KILL-SWITCH-RECOVERY", "endpoint-identity-boundary")]
    public void OneSwitchInstance_RejectsASecondEndpointIdentity()
    {
        (KillSwitchTestDriver driver, ObservableTimeProvider time) = CreateDriver();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(driver.AttachDifferentEndpoint);

        Assert.Contains("cannot observe more than one", exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, time.TimerCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-KILL-SWITCH-OBSERVABILITY", "owned-log-context")]
    public async Task Recovery_RestoresTheEndpointLogContextBeforePauseAndRestartAsync()
    {
        var ownContext = new BusLogContext(NullLoggerFactory.Instance);
        var foreignContext = new BusLogContext(NullLoggerFactory.Instance);
        (KillSwitchTestDriver driver, ObservableTimeProvider time) = CreateDriver(
            activationThreshold: 1,
            tripThresholdRatio: 0,
            logContext: ownContext);
        LogContext.Current = foreignContext;

        await driver.ObserveFailureAsync(new InvalidOperationException("trip"));
        await WaitForPauseAsync(driver, 1);
        await WaitForTimerAsync(time, 1);
        time.Advance(TimeSpan.FromSeconds(1));
        await WaitForStartAsync(driver, 1);
        await driver.RecoveryTask.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);

        Assert.Same(ownContext, driver.LogContextSeenByLastPause);
        Assert.Same(ownContext, driver.LogContextSeenByLastRestart);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-KILL-SWITCH-OBSERVABILITY", "caller-log-context-preserved")]
    public async Task ThresholdLogging_DoesNotReplaceTheCallersAmbientLogContextAsync()
    {
        var ownContext = new BusLogContext(NullLoggerFactory.Instance);
        var callerContext = new BusLogContext(NullLoggerFactory.Instance);
        (KillSwitchTestDriver driver, _) = CreateDriver(
            activationThreshold: 1,
            tripThresholdRatio: 0,
            logContext: ownContext);
        LogContext.Current = callerContext;

        await driver.ObserveAttemptAsync();
        Task observation = driver.ObserveMatchingFailureAsync(new InvalidOperationException("trip"));

        Assert.True(observation.IsCompletedSuccessfully);
        Assert.Same(callerContext, LogContext.Current);
        await WaitForPauseAsync(driver, 1);
        await CancelRecoveryAsync(driver);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-KILL-SWITCH-OBSERVATION", "consumer-and-routing-slip-parity")]
    public async Task ConsumerAndRoutingSlipCallbacks_ContributeToTheSameCountersAsync()
    {
        (KillSwitchTestDriver driver, _) = CreateDriver(activationThreshold: 10, tripThresholdRatio: 1);

        await driver.ObserveConsumerAndRoutingSlipCallbacksAsync(new InvalidOperationException("execute"));

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

    private static Task WaitForPauseAsync(KillSwitchTestDriver driver, int count) =>
        driver.WaitForPauseCountAsync(count, OperationTimeout(), TestContext.Current.CancellationToken);

    private static Task WaitForStartAsync(KillSwitchTestDriver driver, int count) =>
        driver.WaitForStartCountAsync(count, OperationTimeout(), TestContext.Current.CancellationToken);

    private static Task WaitForTimerAsync(ObservableTimeProvider time, int count) =>
        time.WaitForTimerCountAsync(count).WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);

    private static Task CancelRecoveryAsync(KillSwitchTestDriver driver) =>
        driver.StopEndpointAsync().WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);

    private static TimeSpan OperationTimeout() =>
        TestConfigurationProvider.ForCurrentTestRun().GetValidatedOptions().OperationTimeout!.Value;
}
