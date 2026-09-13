using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Testing.Internal;

/// <summary>Tracks activity with a rolling timer and notifies connected inactivity observers.</summary>
internal abstract class InactivityTestObserver :
    Connectable<IInactivityObserver>,
    IDisposable,
    IInactivityObservationSource
{
    readonly object _timerLock = new();
    int _activityDetected;
    int _disposed;
    RollingTimer? _inactivityTimer;
    readonly TimeProvider _timeProvider;

    /// <summary>Creates an observer.</summary>
    /// <param name="timeProvider">The clock used by the rolling inactivity timer.</param>
    protected InactivityTestObserver(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <summary>Releases the rolling inactivity timer.</summary>
    public void Dispose()
    {
        lock (_timerLock)
        {
            if (Volatile.Read(ref _disposed) != 0)
                return;

            Volatile.Write(ref _disposed, 1);
            RollingTimer? timer = _inactivityTimer;
            _inactivityTimer = null;
            timer?.Dispose();
        }
    }

    /// <summary>Connects an inactivity observer and registers this instance as its source.</summary>
    /// <param name="observer">The observer to notify.</param>
    /// <returns>A handle that disconnects the observer.</returns>
    public ConnectHandle ConnectInactivityObserver(IInactivityObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        lock (_timerLock)
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);

            var handle = Connect(observer);

            observer.RegisterSource(this);

            return handle;
        }
    }

    /// <summary>Gets whether the timer has elapsed with no activity still in progress.</summary>
    public virtual bool IsInactive
    {
        get
        {
            lock (_timerLock)
                return _inactivityTimer?.Triggered == true && Volatile.Read(ref _activityDetected) == 0;
        }
    }

    /// <summary>Starts the rolling timer used to detect inactivity.</summary>
    /// <param name="inactivityTimeout">The interval without activity that triggers notification.</param>
    protected void StartTimer(TimeSpan inactivityTimeout)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(inactivityTimeout, TimeSpan.Zero);

        lock (_timerLock)
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
            _inactivityTimer?.Dispose();
            _inactivityTimer = new RollingTimer(OnActivityTimeout, inactivityTimeout, null, _timeProvider);
            _inactivityTimer.Start();
        }
    }

    /// <summary>Restarts the inactivity interval and optionally marks activity as in progress.</summary>
    /// <param name="activityDetected">Whether to record activity before restarting the timer.</param>
    /// <param name="cancellationToken">The token used to cancel the restart.</param>
    /// <returns>A completed task after the timer has restarted.</returns>
    public Task RestartTimerAsync(bool activityDetected = true, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        lock (_timerLock)
        {
            if (Volatile.Read(ref _disposed) != 0)
                return Task.CompletedTask;

            if (activityDetected)
                Interlocked.CompareExchange(ref _activityDetected, 1, 0);

            (_inactivityTimer ?? throw new InvalidOperationException("The inactivity timer has not been started.")).Restart();
        }

        return Task.CompletedTask;
    }

    /// <summary>Notifies every connected observer that this source reached its inactivity interval.</summary>
    /// <returns>A task that completes after all observers have been notified.</returns>
    protected Task NotifyInactiveAsync()
    {
        return ForEachAsync(observer => observer.EvaluateInactivityAsync());
    }

    void OnActivityTimeout(object? state)
    {
        lock (_timerLock)
        {
            if (Volatile.Read(ref _disposed) != 0 || _inactivityTimer == null)
                return;

            _inactivityTimer.Stop();
            Interlocked.CompareExchange(ref _activityDetected, 0, 1);
        }

        try
        {
            NotifyInactiveAsync().GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            LogContext.Error?.Log(exception, "Test inactivity observer notification faulted");
        }
    }
}
