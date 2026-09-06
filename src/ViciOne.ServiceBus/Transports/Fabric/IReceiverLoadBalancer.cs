namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>Defines the operations required by receiver load balancer.</summary>
/// <typeparam name="T">The value type.</typeparam>
public interface IReceiverLoadBalancer<in T>
    where T : class
{
    /// <summary>Selects receiver.</summary>
    /// <param name="message">The message to process.</param>
    /// <returns>The selected receiver.</returns>
    IMessageReceiver<T> SelectReceiver(T message);
}
