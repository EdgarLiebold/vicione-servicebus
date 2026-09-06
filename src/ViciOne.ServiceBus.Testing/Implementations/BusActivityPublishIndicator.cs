using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>An activity indicator for publish endpoints. Utilizes a timer that restarts on publish activity.</summary>
public class BusActivityPublishIndicator : BaseBusActivityIndicatorConnectable,
    IDisposable,
    ISignalResource,
    IPublishObserver
{
    readonly RollingTimer _receiveIdleTimer;
    readonly ISignalResource? _signalResource;
    int _activityStarted;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="signalResource">The signal resource.</param>
    /// <param name="receiveIdleTimeout">The receive idle timeout.</param>
    public BusActivityPublishIndicator(ISignalResource? signalResource, TimeSpan receiveIdleTimeout)
        : this(signalResource, receiveIdleTimeout, TimeProvider.System)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="signalResource">The signal resource.</param>
    /// <param name="receiveIdleTimeout">The receive idle timeout.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    public BusActivityPublishIndicator(ISignalResource? signalResource, TimeSpan receiveIdleTimeout, TimeProvider timeProvider)
    {
        _signalResource = signalResource;
        _receiveIdleTimer = new RollingTimer(SignalInactivity, receiveIdleTimeout, null, timeProvider);
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="signalResource">The signal resource.</param>
    public BusActivityPublishIndicator(ISignalResource? signalResource)
        :
        this(signalResource, TimeSpan.FromSeconds(5))
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="receiveIdleTimeout">The receive idle timeout.</param>
    public BusActivityPublishIndicator(TimeSpan receiveIdleTimeout)
        :
        this(null, receiveIdleTimeout)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    public BusActivityPublishIndicator()
        :
        this(null)
    {
    }

    /// <summary>Gets a value indicating whether met.</summary>
    public override bool IsMet =>
        _receiveIdleTimer.Triggered ||
        Interlocked.CompareExchange(ref _activityStarted, int.MinValue, int.MinValue) == 0;

    /// <summary>Signals the configured condition.</summary>
    public void Signal()
    {
        SignalInactivity(null);
    }

    /// <summary>Releases the resources owned by this instance.</summary>
    public void Dispose()
    {
        _receiveIdleTimer.Dispose();
    }

    /// <summary>Runs before publish.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PrePublishAsync<T>(PublishContext<T> context)
        where T : class
    {
        Interlocked.CompareExchange(ref _activityStarted, 1, 0);
        _receiveIdleTimer.Restart();
        return Task.CompletedTask;
    }

    /// <summary>Runs after publish.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PostPublishAsync<T>(PublishContext<T> context)
        where T : class
    {
        _receiveIdleTimer.Restart();
        return Task.CompletedTask;
    }

    /// <summary>Publishes fault.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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
