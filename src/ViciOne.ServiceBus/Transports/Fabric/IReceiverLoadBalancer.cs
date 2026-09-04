namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>
/// Defines the contract for receiver load balancer.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public interface IReceiverLoadBalancer<in T>
    where T : class
{
    /// <summary>
    /// Performs the select receiver operation.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <returns>The result of the operation.</returns>
    IMessageReceiver<T> SelectReceiver(T message);
}
