using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>
/// Provides a bus test receive observer implementation.
/// </summary>
public class BusTestReceiveObserver :
    InactivityTestObserver,
    IReceiveObserver
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="inactivityTimout">The inactivity timout value.</param>
    public BusTestReceiveObserver(TimeSpan inactivityTimout)
        : this(inactivityTimout, TimeProvider.System)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="inactivityTimout">The inactivity timout value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    public BusTestReceiveObserver(TimeSpan inactivityTimout, TimeProvider timeProvider)
        : base(timeProvider)
    {
        StartTimer(inactivityTimout);
    }

    /// <summary>
    /// Performs the pre receive operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task PreReceiveAsync(ReceiveContext context)
    {
        return RestartTimerAsync();
    }

    /// <summary>
    /// Performs the post receive operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task PostReceiveAsync(ReceiveContext context)
    {
        return RestartTimerAsync(false);
    }

    /// <summary>
    /// Performs the post consume operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="duration">The duration value.</param>
    /// <param name="consumerType">The consumer type value.</param>
    /// <returns>The result of the operation.</returns>
    public Task PostConsumeAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType)
        where T : class
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Consumes fault.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="duration">The duration value.</param>
    /// <param name="consumerType">The consumer type value.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task ConsumeFaultAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception)
        where T : class
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Performs the receive fault operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task ReceiveFaultAsync(ReceiveContext context, Exception exception)
    {
        return RestartTimerAsync(false);
    }
}
