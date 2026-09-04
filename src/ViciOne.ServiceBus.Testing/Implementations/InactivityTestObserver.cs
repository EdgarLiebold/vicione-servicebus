using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>
/// Provides an inactivity test observer implementation.
/// </summary>
public abstract class InactivityTestObserver :
    Connectable<IInactivityObserver>,
    IDisposable,
    IInactivityObservationSource
{
    int _activityDetected;
    RollingTimer? _inactivityTimer;
    TimeProvider _timeProvider = TimeProvider.System;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    protected InactivityTestObserver()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="timeProvider">The time provider value.</param>
    protected InactivityTestObserver(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    public void Dispose()
    {
        _inactivityTimer?.Dispose();
    }

    /// <summary>
    /// Connects inactivity observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectInactivityObserver(IInactivityObserver observer)
    {
        var handle = Connect(observer);

        observer.Connected(this);

        return handle;
    }

    /// <summary>
    /// Gets the is inactive value.
    /// </summary>
    public virtual bool IsInactive => _inactivityTimer?.Triggered == true && _activityDetected == 0;

    /// <summary>
    /// Starts timer.
    /// </summary>
    /// <param name="inactivityTimout">The inactivity timout value.</param>
    protected void StartTimer(TimeSpan inactivityTimout)
    {
        _inactivityTimer = new RollingTimer(OnActivityTimeout, inactivityTimout, null, _timeProvider);
        _inactivityTimer.Start();
    }

    /// <summary>
    /// Performs the restart timer operation.
    /// </summary>
    /// <param name="activityDetected">The activity detected value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task RestartTimerAsync(bool activityDetected = true, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); if (activityDetected)
            Interlocked.CompareExchange(ref _activityDetected, 1, 0);

        (_inactivityTimer ?? throw new InvalidOperationException("The inactivity timer has not been started.")).Restart();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Performs the notify inactive operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
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
