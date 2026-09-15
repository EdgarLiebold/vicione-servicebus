using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Waits for a completed action's trace to remain idle, bounded by its original observation deadline.</summary>
sealed class TrackedActivity :
    IDisposable
{
    static readonly ActivitySource _source = new ActivitySource("ViciOne.ServiceBus.Testing.Monitor");

    readonly HashSet<string> _activeSpans = new HashSet<string>(StringComparer.Ordinal);
    readonly TaskCompletionSource<bool> _completed;
    readonly TimeSpan _idleTimeout;
    readonly ActivityListener _listener;
    readonly object _lock = new object();
    readonly long _startedTimestamp;
    readonly Activity? _testActivity;
    readonly TimeSpan _timeout;
    readonly TimeProvider _timeProvider;
    readonly RollingTimer _timer;
    bool _actionCompleted;
    bool _disposed;
    TimeSpan? _idleSince;

    public TrackedActivity(string? methodName, TimeSpan? timeout, TimeSpan? idleTimeout, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        _idleTimeout = idleTimeout ?? TimeSpan.FromSeconds(0.05);
        _timeout = timeout ?? TimeSpan.FromSeconds(30);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(_idleTimeout, TimeSpan.Zero, nameof(idleTimeout));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(_timeout, TimeSpan.Zero, nameof(timeout));

        _completed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _timeProvider = timeProvider;
        _startedTimestamp = timeProvider.GetTimestamp();
        _timer = new RollingTimer(OnTimeout, _timeout, this, timeProvider);

        _listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = Sample,
            ActivityStarted = ActivityStarted,
            ActivityStopped = ActivityStopped
        };

        try
        {
            ActivitySource.AddActivityListener(_listener);
            _testActivity = _source.CreateActivity($"{methodName ?? "test"} process", ActivityKind.Internal)
                ?? throw new InvalidOperationException("The test activity could not be started.");
            _testActivity.Start();

            lock (_lock)
                ScheduleTimer(_timeProvider.GetElapsedTime(_startedTimestamp));
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public ActivityTraceId TraceId => _testActivity?.TraceId
        ?? throw new InvalidOperationException("The test activity is unavailable.");

    public void StopWaiting()
    {
        lock (_lock)
            _completed.TrySetResult(true);
    }

    public void ActionCompleted()
    {
        lock (_lock)
        {
            if (_disposed || _completed.Task.IsCompleted || _actionCompleted)
                return;

            _actionCompleted = true;
            TimeSpan elapsed = _timeProvider.GetElapsedTime(_startedTimestamp);
            if (_activeSpans.Count == 0)
                _idleSince = elapsed;
            ScheduleTimer(elapsed);
        }
    }

    public async Task WaitForCompletionAsync(CancellationToken cancellationToken = default)
    {
        ActionCompleted();
        await _completed.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
                return;

            _disposed = true;
            _activeSpans.Clear();
            _completed.TrySetResult(true);
        }

        try
        {
            _testActivity?.Dispose();
        }
        finally
        {
            try
            {
                _listener.Dispose();
            }
            finally
            {
                _timer.Dispose();
            }
        }
    }

    void OnTimeout(object? state)
    {
        lock (_lock)
        {
            if (!_disposed && !_completed.Task.IsCompleted)
                ScheduleTimer(_timeProvider.GetElapsedTime(_startedTimestamp));
        }
    }

    static ActivitySamplingResult Sample(ref ActivityCreationOptions<System.Diagnostics.ActivityContext> options)
    {
        return ActivitySamplingResult.AllDataAndRecorded;
    }

    void ActivityStarted(Activity activity)
    {
        lock (_lock)
        {
            if (_disposed || _completed.Task.IsCompleted || !IsRelatedSpan(activity)
                || activity.Id is not string spanId || !_activeSpans.Add(spanId))
                return;

            _idleSince = null;
            ScheduleTimer(_timeProvider.GetElapsedTime(_startedTimestamp));
        }
    }

    void ActivityStopped(Activity activity)
    {
        lock (_lock)
        {
            if (_disposed || _completed.Task.IsCompleted || !IsRelatedSpan(activity)
                || activity.Id is not string spanId || !_activeSpans.Remove(spanId))
                return;

            if (_actionCompleted && _activeSpans.Count == 0)
            {
                TimeSpan elapsed = _timeProvider.GetElapsedTime(_startedTimestamp);
                _idleSince = elapsed;
                ScheduleTimer(elapsed);
            }
        }
    }

    bool IsRelatedSpan(Activity activity)
    {
        return _testActivity != null && !ReferenceEquals(activity, _testActivity)
            && string.Equals(activity.RootId, _testActivity.RootId, StringComparison.Ordinal);
    }

    // Called only while holding _lock; queued callbacks must recheck the current trace state and deadlines.
    void ScheduleTimer(TimeSpan elapsed)
    {
        TimeSpan remaining = _timeout - elapsed;
        if (_actionCompleted && _activeSpans.Count == 0 && _idleSince.HasValue)
        {
            TimeSpan idleRemaining = _idleTimeout - (elapsed - _idleSince.Value);
            if (idleRemaining < remaining)
                remaining = idleRemaining;
        }

        if (remaining <= TimeSpan.Zero)
            _completed.TrySetResult(true);
        else
            _timer.Restart(remaining);
    }
}
