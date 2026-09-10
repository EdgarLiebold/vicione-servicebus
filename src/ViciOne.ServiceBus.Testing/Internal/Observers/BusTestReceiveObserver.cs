using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing.Internal;

/// <summary>Tracks receive-pipeline activity for harness inactivity detection.</summary>
internal sealed class BusTestReceiveObserver :
    InactivityTestObserver,
    IReceiveObserver
{
    /// <summary>Creates a receive observer that uses the system clock.</summary>
    /// <param name="inactivityTimeout">The interval without receive activity that indicates inactivity.</param>
    public BusTestReceiveObserver(TimeSpan inactivityTimeout)
        : this(inactivityTimeout, TimeProvider.System)
    {
    }

    /// <summary>Creates a receive observer.</summary>
    /// <param name="inactivityTimeout">The interval without receive activity that indicates inactivity.</param>
    /// <param name="timeProvider">The clock used by the inactivity timer.</param>
    public BusTestReceiveObserver(TimeSpan inactivityTimeout, TimeProvider timeProvider)
        : base(timeProvider)
    {
        StartTimer(inactivityTimeout);
    }

    /// <summary>Marks receive activity as in progress and restarts the inactivity interval.</summary>
    /// <param name="context">The receive context entering the pipeline.</param>
    /// <returns>A completed task after the timer has restarted.</returns>
    public Task PreReceiveAsync(ReceiveContext context)
    {
        return RestartTimerAsync();
    }

    /// <summary>Marks the receive attempt as complete and restarts the inactivity interval.</summary>
    /// <param name="context">The receive context leaving the pipeline.</param>
    /// <returns>A completed task after the timer has restarted.</returns>
    public Task PostReceiveAsync(ReceiveContext context)
    {
        return RestartTimerAsync(false);
    }

    /// <summary>Accepts the receive-observer callback for a completed consumer invocation.</summary>
    /// <typeparam name="TMessage">The consumed message type.</typeparam>
    /// <param name="context">The completed consume context.</param>
    /// <param name="duration">The consumer invocation duration.</param>
    /// <param name="consumerType">The invoked consumer type name.</param>
    /// <returns>A completed task.</returns>
    public Task PostConsumeAsync<TMessage>(ConsumeContext<TMessage> context, TimeSpan duration, string consumerType)
        where TMessage : class
    {
        return Task.CompletedTask;
    }

    /// <summary>Accepts the receive-observer callback for a faulted consumer invocation.</summary>
    /// <typeparam name="TMessage">The consumed message type.</typeparam>
    /// <param name="context">The faulted consume context.</param>
    /// <param name="duration">The consumer invocation duration.</param>
    /// <param name="consumerType">The invoked consumer type name.</param>
    /// <param name="exception">The exception raised by the consumer pipeline.</param>
    /// <returns>A completed task.</returns>
    public Task ConsumeFaultAsync<TMessage>(ConsumeContext<TMessage> context, TimeSpan duration, string consumerType, Exception exception)
        where TMessage : class
    {
        return Task.CompletedTask;
    }

    /// <summary>Marks a faulted receive attempt as complete and restarts the inactivity interval.</summary>
    /// <param name="context">The receive context that faulted.</param>
    /// <param name="exception">The exception raised by the receive pipeline.</param>
    /// <returns>A completed task after the timer has restarted.</returns>
    public Task ReceiveFaultAsync(ReceiveContext context, Exception exception)
    {
        return RestartTimerAsync(false);
    }
}
