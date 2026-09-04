using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing.Implementations;

public class BusTestReceiveObserver :
    InactivityTestObserver,
    IReceiveObserver
{
    public BusTestReceiveObserver(TimeSpan inactivityTimout)
        : this(inactivityTimout, TimeProvider.System)
    {
    }

    public BusTestReceiveObserver(TimeSpan inactivityTimout, TimeProvider timeProvider)
        : base(timeProvider)
    {
        StartTimer(inactivityTimout);
    }

    public Task PreReceiveAsync(ReceiveContext context)
    {
        return RestartTimerAsync();
    }

    public Task PostReceiveAsync(ReceiveContext context)
    {
        return RestartTimerAsync(false);
    }

    public Task PostConsumeAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType)
        where T : class
    {
        return Task.CompletedTask;
    }

    public Task ConsumeFaultAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception)
        where T : class
    {
        return Task.CompletedTask;
    }

    public Task ReceiveFaultAsync(ReceiveContext context, Exception exception)
    {
        return RestartTimerAsync(false);
    }
}
