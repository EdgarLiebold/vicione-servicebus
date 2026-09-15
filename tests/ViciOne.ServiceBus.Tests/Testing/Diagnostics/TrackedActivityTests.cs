using System.Diagnostics;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;
using ActivityContext = System.Diagnostics.ActivityContext;

namespace ViciOne.ServiceBus.Tests.Testing.Diagnostics;

[Collection(OpenTelemetryGlobalCollection.Name)]
public sealed class TrackedActivityTests
{
    private static readonly DateTimeOffset StartTime = new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan MaximumDuration = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan IdleDuration = TimeSpan.FromMinutes(1);

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-TELEMETRY", "unrelated-traces-do-not-restart-idle")]
    public async Task UnrelatedActivity_DoesNotMoveTheExactIdleDeadlineAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        using var cancellation = new CancellationTokenSource();
        using var tracker = new TrackedActivity(nameof(UnrelatedActivity_DoesNotMoveTheExactIdleDeadlineAsync),
            MaximumDuration, IdleDuration, timeProvider);
        using var source = new ActivitySource("ViciOne.ServiceBus.Tests.UnrelatedTrace");
        Task wait = tracker.WaitForCompletionAsync(cancellation.Token);
        try
        {
            int initialChanges = timeProvider.ChangeCount;
            timeProvider.Advance(IdleDuration / 2);
            using (Activity unrelated = Assert.IsType<Activity>(source.StartActivity("unrelated", ActivityKind.Internal,
                       new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded))))
            {
                Assert.NotEqual(tracker.TraceId, unrelated.TraceId);
            }

            Assert.Equal(initialChanges, timeProvider.ChangeCount);
            timeProvider.Advance(IdleDuration / 2 - TimeSpan.FromTicks(1));
            Assert.False(wait.IsCompleted);
            timeProvider.Advance(TimeSpan.FromTicks(1));
            await wait.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);
        }
        finally
        {
            await CancelAndDrainAsync(cancellation, wait);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-TELEMETRY", "late-related-spans-invalidate-idle")]
    public async Task LateRelatedActivity_PreventsCompletionAtTheOldIdleDeadlineAndStartsANewIdlePeriodAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        using var cancellation = new CancellationTokenSource();
        using var tracker = new TrackedActivity(nameof(LateRelatedActivity_PreventsCompletionAtTheOldIdleDeadlineAndStartsANewIdlePeriodAsync),
            MaximumDuration, IdleDuration, timeProvider);
        using var source = new ActivitySource("ViciOne.ServiceBus.Tests.LateRelatedTrace");
        Activity root = Assert.IsType<Activity>(Activity.Current);
        Task wait = tracker.WaitForCompletionAsync(cancellation.Token);
        try
        {
            timeProvider.Advance(IdleDuration / 2);
            using Activity child = Assert.IsType<Activity>(source.StartActivity("late child", ActivityKind.Internal, root.Context));
            Assert.Equal(tracker.TraceId, child.TraceId);
            Assert.Equal(MaximumDuration - IdleDuration / 2, timeProvider.LastDueTime);

            timeProvider.Advance(IdleDuration / 2);
            Assert.False(wait.IsCompleted);
            child.Stop();
            Assert.Equal(IdleDuration, timeProvider.LastDueTime);
            timeProvider.Advance(IdleDuration - TimeSpan.FromTicks(1));
            Assert.False(wait.IsCompleted);
            timeProvider.Advance(TimeSpan.FromTicks(1));
            await wait.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);
        }
        finally
        {
            await CancelAndDrainAsync(cancellation, wait);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-TELEMETRY", "maximum-duration-is-not-extended-by-idle")]
    public async Task RelatedActivityStoppingNearTheMaximumDeadline_DoesNotExtendTheObservationTimeoutAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        using var cancellation = new CancellationTokenSource();
        using var tracker = new TrackedActivity(nameof(RelatedActivityStoppingNearTheMaximumDeadline_DoesNotExtendTheObservationTimeoutAsync),
            MaximumDuration, IdleDuration, timeProvider);
        using var source = new ActivitySource("ViciOne.ServiceBus.Tests.MaximumTraceDuration");
        Activity root = Assert.IsType<Activity>(Activity.Current);
        using Activity child = Assert.IsType<Activity>(source.StartActivity("active child", ActivityKind.Internal, root.Context));
        Task wait = tracker.WaitForCompletionAsync(cancellation.Token);
        try
        {
            timeProvider.Advance(MaximumDuration - IdleDuration / 2);
            Assert.False(wait.IsCompleted);
            child.Stop();
            Assert.Equal(IdleDuration / 2, timeProvider.LastDueTime);
            timeProvider.Advance(IdleDuration / 2 - TimeSpan.FromTicks(1));
            Assert.False(wait.IsCompleted);
            timeProvider.Advance(TimeSpan.FromTicks(1));
            await wait.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);
        }
        finally
        {
            await CancelAndDrainAsync(cancellation, wait);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-TELEMETRY", "idle-begins-only-after-action-completes")]
    public async Task RelatedActivityStoppingBeforeTheActionCompletes_DoesNotArmAnEarlyIdleDeadlineAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        using var cancellation = new CancellationTokenSource();
        using var tracker = new TrackedActivity(nameof(RelatedActivityStoppingBeforeTheActionCompletes_DoesNotArmAnEarlyIdleDeadlineAsync),
            MaximumDuration, IdleDuration, timeProvider);
        using var source = new ActivitySource("ViciOne.ServiceBus.Tests.ActionCompletionTrace");
        Activity root = Assert.IsType<Activity>(Activity.Current);
        using (Activity child = Assert.IsType<Activity>(source.StartActivity("completed child", ActivityKind.Internal, root.Context)))
        {
            Assert.Equal(tracker.TraceId, child.TraceId);
        }

        Assert.Equal(MaximumDuration, timeProvider.LastDueTime);
        timeProvider.Advance(IdleDuration);
        Task wait = tracker.WaitForCompletionAsync(cancellation.Token);
        try
        {
            Assert.False(wait.IsCompleted);
            Assert.Equal(IdleDuration, timeProvider.LastDueTime);
            timeProvider.Advance(IdleDuration - TimeSpan.FromTicks(1));
            Assert.False(wait.IsCompleted);
            timeProvider.Advance(TimeSpan.FromTicks(1));
            await wait.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);
        }
        finally
        {
            await CancelAndDrainAsync(cancellation, wait);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-TELEMETRY", "queued-idle-callback-rechecks-active-trace")]
    public async Task QueuedIdleCallback_DoesNotCompleteAnActiveLateChildOrMoveTheAbsoluteDeadlineAsync()
    {
        var timeProvider = new QueuedCallbackTimeProvider(StartTime);
        using var cancellation = new CancellationTokenSource();
        using var tracker = new TrackedActivity(nameof(QueuedIdleCallback_DoesNotCompleteAnActiveLateChildOrMoveTheAbsoluteDeadlineAsync),
            MaximumDuration, IdleDuration, timeProvider);
        using var source = new ActivitySource("ViciOne.ServiceBus.Tests.QueuedIdleTrace");
        Activity root = Assert.IsType<Activity>(Activity.Current);
        Task wait = tracker.WaitForCompletionAsync(cancellation.Token);
        try
        {
            Action queuedCallback = timeProvider.CaptureQueuedCallback();
            timeProvider.Advance(IdleDuration / 2);
            using Activity child = Assert.IsType<Activity>(source.StartActivity("late child", ActivityKind.Internal, root.Context));
            timeProvider.Advance(IdleDuration / 2);
            queuedCallback();

            Assert.Equal(MaximumDuration - IdleDuration, timeProvider.LastDueTime);
            Assert.False(wait.IsCompleted);
            timeProvider.Advance(MaximumDuration - IdleDuration - TimeSpan.FromTicks(1));
            Assert.False(wait.IsCompleted);
            timeProvider.Advance(TimeSpan.FromTicks(1));
            await wait.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);
        }
        finally
        {
            await CancelAndDrainAsync(cancellation, wait);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-TELEMETRY", "queued-callback-after-disposal-does-not-rearm")]
    public async Task CallbackDeliveredAfterDisposal_DoesNotRecreateATimerOrRetainTheGlobalListenerAsync()
    {
        var timeProvider = new QueuedCallbackTimeProvider(StartTime);
        using var cancellation = new CancellationTokenSource();
        using var tracker = new TrackedActivity(nameof(CallbackDeliveredAfterDisposal_DoesNotRecreateATimerOrRetainTheGlobalListenerAsync),
            MaximumDuration, IdleDuration, timeProvider);
        using var source = new ActivitySource("ViciOne.ServiceBus.Tests.DisposedTrace");
        Task wait = tracker.WaitForCompletionAsync(cancellation.Token);
        try
        {
            Action queuedCallback = timeProvider.CaptureQueuedCallback();
            tracker.Dispose();
            Assert.Equal(0, timeProvider.ActiveTimerCount);
            queuedCallback();
            tracker.Dispose();

            Assert.Equal(1, timeProvider.TimerCount);
            Assert.Equal(0, timeProvider.ActiveTimerCount);
            Assert.Null(source.StartActivity("after disposal"));
        }
        finally
        {
            await CancelAndDrainAsync(cancellation, wait);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-TELEMETRY", "timer-creation-failure-releases-activity-and-listener")]
    public void TimerCreationFailure_PreservesTheExactExceptionAndRestoresTheAmbientActivityWithoutALeakedListener()
    {
        var expected = new InvalidOperationException("selected timer creation failure");
        var timeProvider = new QueuedCallbackTimeProvider(StartTime, expected);
        using var ambient = new Activity("existing ambient activity");
        ambient.Start();
        using var source = new ActivitySource("ViciOne.ServiceBus.Tests.FailedTrackerConstruction");

        InvalidOperationException actual = Assert.Throws<InvalidOperationException>(() =>
            new TrackedActivity(nameof(TimerCreationFailure_PreservesTheExactExceptionAndRestoresTheAmbientActivityWithoutALeakedListener),
                MaximumDuration, IdleDuration, timeProvider));

        Assert.Same(expected, actual);
        Assert.Same(ambient, Activity.Current);
        Assert.Equal(0, timeProvider.TimerCount);
        Assert.Equal(0, timeProvider.ActiveTimerCount);
        Assert.Null(source.StartActivity("after constructor failure"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-TELEMETRY", "root-start-callback-child-is-tracked")]
    public async Task ChildCreatedByARootStartListener_RemainsActiveUntilItStopsAndThenBeginsTheExactIdlePeriodAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        using var cancellation = new CancellationTokenSource();
        using var source = new ActivitySource("ViciOne.ServiceBus.Tests.ConstructorChildTrace");
        Activity? child = null;
        using var listener = new ActivityListener
        {
            ShouldListenTo = activitySource => activitySource.Name == "ViciOne.ServiceBus.Testing.Monitor",
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = root => child = source.StartActivity("child during root start", ActivityKind.Internal, root.Context),
        };
        ActivitySource.AddActivityListener(listener);
        using var tracker = new TrackedActivity(nameof(ChildCreatedByARootStartListener_RemainsActiveUntilItStopsAndThenBeginsTheExactIdlePeriodAsync),
            MaximumDuration, IdleDuration, timeProvider);
        Task wait = tracker.WaitForCompletionAsync(cancellation.Token);
        try
        {
            Activity actualChild = Assert.IsType<Activity>(child);
            Assert.Equal(tracker.TraceId, actualChild.TraceId);
            Assert.Equal(MaximumDuration, timeProvider.LastDueTime);
            timeProvider.Advance(IdleDuration);
            Assert.False(wait.IsCompleted);
            actualChild.Stop();
            Assert.Equal(IdleDuration, timeProvider.LastDueTime);
            timeProvider.Advance(IdleDuration - TimeSpan.FromTicks(1));
            Assert.False(wait.IsCompleted);
            timeProvider.Advance(TimeSpan.FromTicks(1));
            await wait.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);
        }
        finally
        {
            await CancelAndDrainAsync(cancellation, wait);
            child?.Dispose();
        }
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions().OperationTimeout!.Value;

    private static async Task CancelAndDrainAsync(CancellationTokenSource cancellation, Task wait)
    {
        await cancellation.CancelAsync();
        try
        {
            await wait.WaitAsync(OperationTimeout(), CancellationToken.None);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
    }
}
