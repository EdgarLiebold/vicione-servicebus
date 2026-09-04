using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>
/// An activity indicator for send endpoints. Utilizes a timer that restarts on send activity.
/// </summary>
public class BusActivitySendIndicator : BaseBusActivityIndicatorConnectable,
    IDisposable,
    ISignalResource,
    ISendObserver
{
    readonly RollingTimer _receiveIdleTimer;
    readonly ISignalResource? _signalResource;
    int _activityStarted;

    public BusActivitySendIndicator(ISignalResource? signalResource, TimeSpan receiveIdleTimeout)
        : this(signalResource, receiveIdleTimeout, TimeProvider.System)
    {
    }

    public BusActivitySendIndicator(ISignalResource? signalResource, TimeSpan receiveIdleTimeout, TimeProvider timeProvider)
    {
        _signalResource = signalResource;
        _receiveIdleTimer = new RollingTimer(SignalInactivity, receiveIdleTimeout, null, timeProvider);
    }

    public BusActivitySendIndicator(ISignalResource? signalResource)
        :
        this(signalResource, TimeSpan.FromSeconds(5))
    {
    }

    public BusActivitySendIndicator(TimeSpan receiveIdleTimeout)
        :
        this(null, receiveIdleTimeout)
    {
    }

    public BusActivitySendIndicator()
        :
        this(null)
    {
    }

    public override bool IsMet =>
        _receiveIdleTimer.Triggered ||
        Interlocked.CompareExchange(ref _activityStarted, int.MinValue, int.MinValue) == 0;

    public void Signal()
    {
        SignalInactivity(null);
    }

    public void Dispose()
    {
        _receiveIdleTimer.Dispose();
    }

    public Task PreSendAsync<T>(SendContext<T> context)
        where T : class
    {
        Interlocked.CompareExchange(ref _activityStarted, 1, 0);
        _receiveIdleTimer.Restart();
        return Task.CompletedTask;
    }

    public Task PostSendAsync<T>(SendContext<T> context)
        where T : class
    {
        _receiveIdleTimer.Restart();
        return Task.CompletedTask;
    }

    public Task SendFaultAsync<T>(SendContext<T> context, Exception exception)
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
