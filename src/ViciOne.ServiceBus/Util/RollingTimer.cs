using System;
using System.Threading;

namespace ViciOne.ServiceBus.Util;

/// <summary>
/// Thread safe timer that allows efficient restarts by rolling the due time further into the future.
/// Will roll over once every 43~ days of continuous runtime without a restart.
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

    /// <summary>Initializes a new instance.</summary>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="state">The state.</param>
    public RollingTimer(TimerCallback callback, TimeSpan timeout, object? state = default)
        : this(callback, timeout, state, TimeProvider.System)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="state">The state.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
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

    /// <summary>Gets the triggered.</summary>
    public bool Triggered => _triggered == 1;

    /// <summary>Releases the resources owned by this instance.</summary>
    public void Dispose()
    {
        lock (_lock)
        {
            _timer?.Dispose();
            _timer = null;
        }
    }

    /// <summary>Creates a new timer and starts it.</summary>
    public void Start()
    {
        lock (_lock)
        {
            _timer?.Dispose();
            StartInternal();
        }
    }

    /// <summary>Stops and disposes the existing timer.</summary>
    public void Stop()
    {
        Dispose();
    }

    /// <summary>Restarts the existing timer, creates and starts a new timer if it does not exist.</summary>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
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

    /// <summary>Sets the timer as triggered.</summary>
    void Set()
    {
        Interlocked.CompareExchange(ref _triggered, 1, 0);
    }

    /// <summary>Resets the trigger status.</summary>
    void Reset()
    {
        Interlocked.CompareExchange(ref _triggered, 0, 1);
    }
}
