using System;
using System.Threading;

namespace ViciOne.ServiceBus.Util;

/// <summary>
/// A restartable one-shot timer backed by a configurable time source.
/// </summary>
public class RollingTimer :
    IDisposable
{
    readonly TimerCallback _callback;
    readonly object _lock = new object();
    readonly object? _state;
    readonly TimeProvider _timeProvider;
    TimeSpan _timeout;
    ITimer? _timer;
    int _triggered;

    /// <summary>Creates a timer using the system time source; starting or restarting begins its countdown.</summary>
    /// <param name="callback">The callback invoked when the timer becomes due.</param>
    /// <param name="timeout">The interval used when no replacement interval is supplied.</param>
    /// <param name="state">The state object passed to the callback, or null.</param>
    public RollingTimer(TimerCallback callback, TimeSpan timeout, object? state = default)
        : this(callback, timeout, state, TimeProvider.System)
    {
    }

    /// <summary>Creates a timer using the supplied time source; starting or restarting begins its countdown.</summary>
    /// <param name="callback">The callback invoked when the timer becomes due.</param>
    /// <param name="timeout">The interval used when no replacement interval is supplied.</param>
    /// <param name="state">The state object passed to the callback, or null.</param>
    /// <param name="timeProvider">The time source that creates and schedules the timer.</param>
    public RollingTimer(TimerCallback callback, TimeSpan timeout, object? state, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(callback);
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

        void Callback(object? obj)
        {
            Set();
            callback(obj);
        }

        _callback = Callback;
        _timeout = timeout;
        _state = state;
    }

    /// <summary>Gets whether the timer callback has run since the latest start or restart.</summary>
    public bool Triggered => _triggered == 1;

    /// <summary>Disposes and clears the current timer; a subsequent start or restart may create another.</summary>
    public void Dispose()
    {
        lock (_lock)
        {
            _timer?.Dispose();
            _timer = null;
        }
    }

    /// <summary>Disposes any current timer and starts a new one with the retained interval.</summary>
    public void Start()
    {
        lock (_lock)
        {
            _timer?.Dispose();
            StartInternal();
        }
    }

    /// <summary>Disposes and clears the current timer.</summary>
    public void Stop()
    {
        Dispose();
    }

    /// <summary>Resets the trigger and countdown, creating a timer if none exists.</summary>
    /// <param name="timeout">The replacement interval, or null to reuse the retained interval.</param>
    public void Restart(TimeSpan? timeout = null)
    {
        lock (_lock)
        {
            if (timeout.HasValue)
                _timeout = timeout.Value;

            if (_timer == null)
                StartInternal();
            else
            {
                Reset();
                _timer.Change(_timeout, Timeout.InfiniteTimeSpan);
            }
        }
    }

    void StartInternal()
    {
        Reset();
        _timer = _timeProvider.CreateTimer(_callback, _state, _timeout, Timeout.InfiniteTimeSpan);
    }

    /// <summary>Records that a timer callback has begun.</summary>
    void Set()
    {
        Interlocked.CompareExchange(ref _triggered, 1, 0);
    }

    /// <summary>Clears the callback-triggered state for a new countdown.</summary>
    void Reset()
    {
        Interlocked.CompareExchange(ref _triggered, 0, 1);
    }
}
