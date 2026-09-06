using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>Observes inactivity test events.</summary>
public abstract class InactivityTestObserver :
    Connectable<IInactivityObserver>,
    IDisposable,
    IInactivityObservationSource
{
    int _activityDetected;
    RollingTimer? _inactivityTimer;
    TimeProvider _timeProvider = TimeProvider.System;

    /// <summary>Initializes a new instance.</summary>
    protected InactivityTestObserver()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="timeProvider">The time source used by the operation.</param>
    protected InactivityTestObserver(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <summary>Releases the resources owned by this instance.</summary>
    public void Dispose()
    {
        _inactivityTimer?.Dispose();
    }

    /// <summary>Connects inactivity observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectInactivityObserver(IInactivityObserver observer)
    {
        var handle = Connect(observer);

        observer.Connected(this);

        return handle;
    }

    /// <summary>Gets a value indicating whether inactive.</summary>
    public virtual bool IsInactive => _inactivityTimer?.Triggered == true && _activityDetected == 0;

    /// <summary>Starts timer.</summary>
    /// <param name="inactivityTimout">The inactivity timout.</param>
    protected void StartTimer(TimeSpan inactivityTimout)
    {
        _inactivityTimer = new RollingTimer(OnActivityTimeout, inactivityTimout, null, _timeProvider);
        _inactivityTimer.Start();
    }

    /// <summary>Restarts timer.</summary>
    /// <param name="activityDetected">The activity detected.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task RestartTimerAsync(bool activityDetected = true, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); if (activityDetected)
            Interlocked.CompareExchange(ref _activityDetected, 1, 0);

        (_inactivityTimer ?? throw new InvalidOperationException("The inactivity timer has not been started.")).Restart();

        return Task.CompletedTask;
    }

    /// <summary>Notifies registered observers about inactive.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected Task NotifyInactiveAsync()
    {
        return ForEachAsync(x => x.NoActivityAsync());
    }

    void OnActivityTimeout(object? state)
    {
        _inactivityTimer?.Stop();
        Interlocked.CompareExchange(ref _activityDetected, 0, 1);

        try
        {
            NotifyInactiveAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            LogContext.Error?.Log(exception, "Test inactivity observer notification faulted");
        }
    }
}
