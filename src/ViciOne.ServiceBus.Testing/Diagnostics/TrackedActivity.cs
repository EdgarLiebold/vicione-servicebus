using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Testing;

class TrackedActivity :
    IDisposable
{
    static readonly ActivitySource _source = new ActivitySource("ViciOne.ServiceBus.Testing.Monitor");

    readonly TaskCompletionSource<bool> _completed;
    readonly TimeSpan _idleTimeout;
    readonly ActivityListener _listener;
    readonly Activity? _testActivity;
    readonly TimeSpan _timeout;
    readonly RollingTimer _timer;
    readonly TraceInfo _traceInfo;
    int _disposed;

    public TrackedActivity(string? methodName, TimeSpan? timeout, TimeSpan? idleTimeout, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        _idleTimeout = idleTimeout ?? TimeSpan.FromSeconds(0.05);
        _timeout = timeout ?? TimeSpan.FromSeconds(30);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(_idleTimeout, TimeSpan.Zero, nameof(idleTimeout));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(_timeout, TimeSpan.Zero, nameof(timeout));

        _completed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        _listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = Sample,
            ActivityStarted = ActivityStarted,
            ActivityStopped = ActivityStopped
        };

        ActivitySource.AddActivityListener(_listener);

        _traceInfo = new TraceInfo { StartTime = timeProvider.GetUtcNow() };
        _testActivity = _source.StartActivity($"{methodName ?? "test"} process");
        if (_testActivity == null)
            throw CreateActivityUnavailableException(_listener);
        _traceInfo.StartTime = _testActivity.StartTimeUtc;

        _timer = new RollingTimer(OnTimeout, _timeout, this, timeProvider);
        _timer.Start();
    }

    public ActivityTraceId TraceId => _testActivity?.TraceId
        ?? throw new InvalidOperationException("The test activity is unavailable.");

    public void StopWaiting()
    {
        _completed.TrySetResult(true);
    }

    public void ActionCompleted()
    {
        if (_traceInfo.Spans.All(x => x.Value.Completed))
            _timer.Restart(_idleTimeout);
    }

    public async Task WaitForCompletionAsync(CancellationToken cancellationToken = default)
    {
        ActionCompleted();
        await _completed.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        _testActivity?.Stop();
        _testActivity?.Dispose();
        _listener.Dispose();
        _timer.Dispose();
    }

    void OnTimeout(object? state)
    {
        _completed.TrySetResult(true);
    }

    static ActivitySamplingResult Sample(ref ActivityCreationOptions<System.Diagnostics.ActivityContext> options)
    {
        return ActivitySamplingResult.AllDataAndRecorded;
    }

    void ActivityStarted(Activity activity)
    {
        if (!ReferenceEquals(activity, _testActivity))
            GetSpan(activity);
    }

    void ActivityStopped(Activity activity)
    {
        if (ReferenceEquals(activity, _testActivity))
            return;

        var span = GetSpan(activity);
        if (span != null)
            span.Completed = true;

        if (_traceInfo.Spans.All(x => x.Value.Completed))
            _timer.Restart(_idleTimeout);
    }

    SpanInfo? GetSpan(Activity activity)
    {
        var traceId = activity.RootId ?? "";
        if (traceId != _testActivity?.RootId)
            return null;

        var span = _traceInfo.Spans.GetOrAdd(activity.Id ?? "", id => new SpanInfo
        {
            SpanId = id,
            ParentId = activity.ParentId,
            StartTime = activity.StartTimeUtc,
            OperationName = activity.OperationName,
            Activity = activity,
        });

        if (activity.Duration > TimeSpan.Zero)
        {
            span.Duration = activity.Duration;

            var traceDuration = activity.StartTimeUtc - _traceInfo.StartTime + activity.Duration;

            if (traceDuration > _traceInfo.Duration)
                _traceInfo.Duration = traceDuration;
        }

        return span;
    }

    static InvalidOperationException CreateActivityUnavailableException(ActivityListener listener)
    {
        listener.Dispose();
        return new InvalidOperationException("The test activity could not be started.");
    }


    sealed class TraceInfo
    {
        public TraceInfo()
        {
            Spans = new ConcurrentDictionary<string, SpanInfo>();
        }

        public DateTimeOffset StartTime { get; set; }
        public TimeSpan Duration { get; set; }

        public ConcurrentDictionary<string, SpanInfo> Spans { get; set; }
    }


    sealed class SpanInfo
    {
        public string? SpanId { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public TimeSpan Duration { get; set; }
        public string? ParentId { get; set; }
        public string? OperationName { get; set; }
        public Activity? Activity { get; set; }
        public bool Completed { get; set; }
    }
}
