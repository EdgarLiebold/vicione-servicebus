using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>
/// Observes receive-pipeline processing, transport settlement, and consumer outcomes at an endpoint.
/// </summary>
public interface IReceiveObserver
{
    /// <summary>Called before the receive pipeline processes a transport delivery.</summary>
    /// <param name="context">The receive context of the message.</param>
    /// <returns>A non-null task that completes after the observation has finished.</returns>
    Task PreReceiveAsync(ReceiveContext context);

    /// <summary>Called when the message has been received and acknowledged on the transport.</summary>
    /// <param name="context">The receive context of the message.</param>
    /// <returns>A non-null task that completes after the observation has finished.</returns>
    Task PostReceiveAsync(ReceiveContext context);

    /// <summary>Called when a message has been consumed by a consumer.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="context">The message consume context.</param>
    /// <param name="duration">The elapsed consumer execution time.</param>
    /// <param name="consumerType">The consumer type name.</param>
    /// <returns>A non-null task that completes after the observation has finished.</returns>
    Task PostConsumeAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType)
        where T : class;

    /// <summary>Called when a message being consumed produced a fault.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="context">The message consume context.</param>
    /// <param name="duration">The elapsed consumer execution time.</param>
    /// <param name="consumerType">The consumer type name.</param>
    /// <param name="exception">The exception from the consumer.</param>
    /// <returns>A non-null task that completes after the observation has finished.</returns>
    Task ConsumeFaultAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception)
        where T : class;

    /// <summary>Called when receive processing or transport settlement faults.</summary>
    /// <param name="context">The receive context of the message.</param>
    /// <param name="exception">The exception that was thrown.</param>
    /// <returns>A non-null task that completes after the observation has finished.</returns>
    Task ReceiveFaultAsync(ReceiveContext context, Exception exception);
}
