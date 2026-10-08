using System.Diagnostics;
using ViciOne.ServiceBus.Logging.Diagnostics;
using ViciOne.ServiceBus.Monitoring;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Testing;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Logging;

[Collection(OpenTelemetryGlobalCollection.Name)]
public sealed class ActivityObservationIsolationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-ISOLATION", "sampler-restore-currentchanged-isolated")]
    public void TryCreate_RestoresCallerDespiteOrdinaryCurrentChangedFailure(bool hostileRestore)
    {
        using var parent = new Activity("sampler caller").Start();
        using var source = new ActivitySource("test.sampler.restore");
        var samplerFailure = new InvalidOperationException("sampler failed");
        int samples = 0;
        int restores = 0;
        using var listener = new ActivityListener
        {
            ShouldListenTo = candidate => ReferenceEquals(candidate, source),
            Sample = (ref ActivityCreationOptions<System.Diagnostics.ActivityContext> _) =>
            {
                samples++;
                Activity.Current = null;
                throw samplerFailure;
            }
        };
        ActivitySource.AddActivityListener(listener);
        void Changed(object? sender, ActivityChangedEventArgs args)
        {
            if (!ReferenceEquals(args.Current, parent))
                return;
            restores++;
            if (hostileRestore)
                throw new InvalidOperationException("restore observer failed");
        }
        Activity.CurrentChanged += Changed;
        try
        {
            Activity? created = null;
            Exception? escaped = Record.Exception(() => created = ActivityObservation.TryCreate(
                new Lazy<ActivitySource>(() => source), "sample", ActivityKind.Internal));

            Assert.Null(escaped);
            Assert.Null(created);
            Assert.Same(parent, Activity.Current);
            Assert.Equal(1, samples);
            Assert.Equal(1, restores);
        }
        finally
        {
            Activity.CurrentChanged -= Changed;
            Activity.Current = parent;
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-ISOLATION", "stop-and-restore-cannot-replace-business-failure")]
    public void Stop_PreservesBusinessFailureWhenStopAndRestoreObserversThrow()
    {
        using var parent = new Activity("business caller").Start();
        using var source = new ActivitySource("test.stop.restore");
        int stops = 0;
        int restores = 0;
        using var listener = new ActivityListener
        {
            ShouldListenTo = candidate => ReferenceEquals(candidate, source),
            Sample = static (ref ActivityCreationOptions<System.Diagnostics.ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = _ =>
            {
                stops++;
                throw new InvalidOperationException("stop observer failed");
            }
        };
        ActivitySource.AddActivityListener(listener);
        Activity activity = Assert.IsType<Activity>(ActivityObservation.TryStartSource(source, "operation", ActivityKind.Internal));
        var started = new StartedActivity(activity);
        var businessFailure = new ApplicationException("real operation failure");
        void Changed(object? sender, ActivityChangedEventArgs args)
        {
            if (!ReferenceEquals(args.Current, parent))
                return;
            restores++;
            throw new InvalidOperationException("restore observer failed");
        }
        Activity.CurrentChanged += Changed;
        try
        {
            Exception? escaped = Record.Exception((Action)(() =>
            {
                try { throw businessFailure; }
                finally { started.Stop(); }
            }));
            Assert.Same(businessFailure, escaped);
            Assert.Same(parent, Activity.Current);
            Assert.True(activity.IsStopped);
            Assert.Equal(ActivityStatusCode.Ok, activity.Status);
            Assert.Equal(1, stops);
            Assert.Equal(1, restores);
            Assert.Null(Record.Exception(started.Dispose));
            Assert.Equal(1, stops);
            Assert.Equal(1, restores);
        }
        finally
        {
            Activity.CurrentChanged -= Changed;
            Activity.Current = parent;
            activity.Dispose();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-ISOLATION", "failed-start-retains-primary-over-cleanup-callbacks")]
    public void TryStart_RetainsOriginalStartFailureWhenCleanupObserversThrow()
    {
        using var parent = new Activity("start caller").Start();
        using var source = new ActivitySource("test.start.cleanup");
        var startFailure = new ApplicationException("original start observer failure");
        int starts = 0;
        int stops = 0;
        using var listener = new ActivityListener
        {
            ShouldListenTo = candidate => ReferenceEquals(candidate, source),
            Sample = static (ref ActivityCreationOptions<System.Diagnostics.ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = _ => { starts++; throw startFailure; },
            ActivityStopped = _ => { stops++; throw new InvalidOperationException("cleanup observer failed"); }
        };
        ActivitySource.AddActivityListener(listener);
        using Activity activity = Assert.IsType<Activity>(source.CreateActivity("operation", ActivityKind.Internal));
        void Changed(object? sender, ActivityChangedEventArgs args)
        {
            if (ReferenceEquals(args.Current, parent))
                throw new InvalidOperationException("cleanup restore failed");
        }
        Activity.CurrentChanged += Changed;
        try
        {
            Exception? reported = null;
            bool started = true;
            Exception? escaped = Record.Exception(() => started = ActivityObservation.TryStart(activity, out reported));
            Assert.Null(escaped);
            Assert.False(started);
            Assert.Same(startFailure, reported);
            Assert.Same(parent, Activity.Current);
            Assert.True(activity.IsStopped);
            Assert.Equal(1, starts);
            Assert.Equal(1, stops);
        }
        finally
        {
            Activity.CurrentChanged -= Changed;
            Activity.Current = parent;
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-ACTIVITY-OUTCOME", "exception-event-clock-failure-preserves-error-status-and-cause")]
    public void AddExceptionEvent_PreservesBusinessFailureAndStatusWhenDiagnosticClockThrows(bool hostileClock)
    {
        using var activity = new Activity("diagnosed operation").Start();
        var clock = new DiagnosticClock(hostileClock);
        var started = new StartedActivity(activity, clock);
        var businessFailure = new ApplicationException("original business failure");

        Exception? escaped = Record.Exception((Action)(() =>
        {
            try { throw businessFailure; }
            finally { started.AddExceptionEvent(businessFailure); }
        }));
        started.Stop();

        Assert.Same(businessFailure, escaped);
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Equal("original business failure", activity.StatusDescription);
        Assert.True(activity.IsStopped);
        Assert.Equal(1, clock.Reads);
        if (hostileClock)
            Assert.Empty(activity.Events);
        else
        {
            ActivityEvent recorded = Assert.Single(activity.Events);
            Assert.Equal(DiagnosticClock.Instant, recorded.Timestamp);
            Assert.Equal(ServiceBusTelemetry.Events.Exception, recorded.Name);
            Assert.Equal("original business failure", recorded.Tags.Single(pair =>
                pair.Key == ServiceBusTelemetry.Attributes.ExceptionMessage).Value);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-ISOLATION", "stopped-parent-restores-nearest-live-ancestor")]
    public void Stop_RestoresNearestLiveAncestorWhenCapturedParentStopped(bool unrelatedCurrent)
    {
        using var grandparent = new Activity("live ancestor").Start();
        using var parent = new Activity("stopped parent").Start();
        using var child = new Activity("child");
        Assert.True(ActivityObservation.TryStart(child));
        parent.Stop();
        // Represent an active caller whose captured parent has already been stopped elsewhere.
        Activity.Current = child;
        Assert.Same(child, Activity.Current);
        using Activity? unrelated = unrelatedCurrent ? new Activity("unrelated live current").Start() : null;

        ActivityObservation.TryDispose(child);

        Assert.True(child.IsStopped);
        Assert.True(parent.IsStopped);
        Assert.False(grandparent.IsStopped);
        Assert.Same(unrelated ?? grandparent, Activity.Current);
    }

    private sealed class DiagnosticClock(bool hostile) : TimeProvider
    {
        public static readonly DateTimeOffset Instant = new(2043, 5, 6, 7, 8, 9, TimeSpan.Zero);
        public int Reads { get; private set; }
        public override DateTimeOffset GetUtcNow()
        {
            Reads++;
            return hostile ? throw new InvalidOperationException("optional diagnostic clock failure") : Instant;
        }
    }
}
