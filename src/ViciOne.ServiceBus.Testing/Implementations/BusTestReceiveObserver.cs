using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>Observes bus test receive events.</summary>
public class BusTestReceiveObserver :
    InactivityTestObserver,
    IReceiveObserver
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="inactivityTimout">The inactivity timout.</param>
    public BusTestReceiveObserver(TimeSpan inactivityTimout)
        : this(inactivityTimout, TimeProvider.System)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="inactivityTimout">The inactivity timout.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    public BusTestReceiveObserver(TimeSpan inactivityTimout, TimeProvider timeProvider)
        : base(timeProvider)
    {
        StartTimer(inactivityTimout);
    }

    /// <summary>Runs before receive.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PreReceiveAsync(ReceiveContext context)
    {
        return RestartTimerAsync();
    }

    /// <summary>Runs after receive.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PostReceiveAsync(ReceiveContext context)
    {
        return RestartTimerAsync(false);
    }

    /// <summary>Runs after consume.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="consumerType">The runtime consumer type used by the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PostConsumeAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType)
        where T : class
    {
        return Task.CompletedTask;
    }

    /// <summary>Consumes fault.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="consumerType">The runtime consumer type used by the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ConsumeFaultAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception)
        where T : class
    {
        return Task.CompletedTask;
    }

    /// <summary>Receives fault.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ReceiveFaultAsync(ReceiveContext context, Exception exception)
    {
        return RestartTimerAsync(false);
    }
}
