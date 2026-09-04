using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>
/// An activity indicator for publish endpoints. Utilizes a timer that restarts on publish activity.
/// </summary>
public class BusActivityPublishIndicator : BaseBusActivityIndicatorConnectable,
    IDisposable,
    ISignalResource,
    IPublishObserver
{
    readonly RollingTimer _receiveIdleTimer;
    readonly ISignalResource? _signalResource;
    int _activityStarted;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="signalResource">The signal resource value.</param>
    /// <param name="receiveIdleTimeout">The receive idle timeout value.</param>
    public BusActivityPublishIndicator(ISignalResource? signalResource, TimeSpan receiveIdleTimeout)
        : this(signalResource, receiveIdleTimeout, TimeProvider.System)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="signalResource">The signal resource value.</param>
    /// <param name="receiveIdleTimeout">The receive idle timeout value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    public BusActivityPublishIndicator(ISignalResource? signalResource, TimeSpan receiveIdleTimeout, TimeProvider timeProvider)
    {
        _signalResource = signalResource;
        _receiveIdleTimer = new RollingTimer(SignalInactivity, receiveIdleTimeout, null, timeProvider);
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="signalResource">The signal resource value.</param>
    public BusActivityPublishIndicator(ISignalResource? signalResource)
        :
        this(signalResource, TimeSpan.FromSeconds(5))
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="receiveIdleTimeout">The receive idle timeout value.</param>
    public BusActivityPublishIndicator(TimeSpan receiveIdleTimeout)
        :
        this(null, receiveIdleTimeout)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public BusActivityPublishIndicator()
        :
        this(null)
    {
    }

    /// <summary>
    /// Gets the is met value.
    /// </summary>
    public override bool IsMet =>
        _receiveIdleTimer.Triggered ||
        Interlocked.CompareExchange(ref _activityStarted, int.MinValue, int.MinValue) == 0;

    /// <summary>
    /// Performs the signal operation.
    /// </summary>
    public void Signal()
    {
        SignalInactivity(null);
    }

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    public void Dispose()
    {
        _receiveIdleTimer.Dispose();
    }

    /// <summary>
    /// Performs the pre publish operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task PrePublishAsync<T>(PublishContext<T> context)
        where T : class
    {
        Interlocked.CompareExchange(ref _activityStarted, 1, 0);
        _receiveIdleTimer.Restart();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Performs the post publish operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task PostPublishAsync<T>(PublishContext<T> context)
        where T : class
    {
        _receiveIdleTimer.Restart();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Publishes fault.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task PublishFaultAsync<T>(PublishContext<T> context, Exception exception)
        where T : class
    {
        _receiveIdleTimer.Restart();
        return Task.CompletedTask;
    }

    void SignalInactivity(object? state)
    {
        _signalResource?.Signal();
        ConditionUpdatedAsync();
        Interlocked.CompareExchange(ref _activityStarted, 0, 1);
        _receiveIdleTimer.Stop();
    }
}
