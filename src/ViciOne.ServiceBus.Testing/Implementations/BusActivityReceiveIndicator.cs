using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>An activity indicator for receive endpoint queues. Utilizes a timer that restarts on receive activity.</summary>
public class BusActivityReceiveIndicator : BaseBusActivityIndicatorConnectable,
    IDisposable,
    ISignalResource,
    IReceiveObserver
{
    readonly RollingTimer _receiveIdleTimer;
    readonly ISignalResource? _signalResource;
    int _activityStarted;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="signalResource">The signal resource.</param>
    /// <param name="receiveIdleTimeout">The receive idle timeout.</param>
    public BusActivityReceiveIndicator(ISignalResource? signalResource, TimeSpan receiveIdleTimeout)
        : this(signalResource, receiveIdleTimeout, TimeProvider.System)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="signalResource">The signal resource.</param>
    /// <param name="receiveIdleTimeout">The receive idle timeout.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    public BusActivityReceiveIndicator(ISignalResource? signalResource, TimeSpan receiveIdleTimeout, TimeProvider timeProvider)
    {
        _signalResource = signalResource;
        _receiveIdleTimer = new RollingTimer(SignalInactivity, receiveIdleTimeout, null, timeProvider);
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="signalResource">The signal resource.</param>
    public BusActivityReceiveIndicator(ISignalResource? signalResource)
        :
        this(signalResource, TimeSpan.FromSeconds(5))
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="receiveIdleTimeout">The receive idle timeout.</param>
    public BusActivityReceiveIndicator(TimeSpan receiveIdleTimeout)
        :
        this(null, receiveIdleTimeout)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    public BusActivityReceiveIndicator()
        :
        this(null)
    {
    }

    /// <summary>Gets a value indicating whether met.</summary>
    public override bool IsMet =>
        _receiveIdleTimer.Triggered ||
        Interlocked.CompareExchange(ref _activityStarted, int.MinValue, int.MinValue) == 0;

    Task IReceiveObserver.PreReceiveAsync(ReceiveContext context)
    {
        Interlocked.CompareExchange(ref _activityStarted, 1, 0);
        _receiveIdleTimer.Restart();
        return Task.CompletedTask;
    }

    Task IReceiveObserver.PostReceiveAsync(ReceiveContext context)
    {
        _receiveIdleTimer.Restart();
        return Task.CompletedTask;
    }

    Task IReceiveObserver.PostConsumeAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType)
    {
        return Task.CompletedTask;
    }

    Task IReceiveObserver.ConsumeFaultAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception)
    {
        return Task.CompletedTask;
    }

    Task IReceiveObserver.ReceiveFaultAsync(ReceiveContext context, Exception exception)
    {
        _receiveIdleTimer.Restart();
        return Task.CompletedTask;
    }

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

    void SignalInactivity(object? state)
    {
        _signalResource?.Signal();
        ConditionUpdatedAsync();
        Interlocked.CompareExchange(ref _activityStarted, 0, 1);
        _receiveIdleTimer.Stop();
    }
}
