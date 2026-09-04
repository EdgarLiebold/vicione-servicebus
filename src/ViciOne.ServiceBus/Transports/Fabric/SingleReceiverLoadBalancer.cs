namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>
/// Provides a single receiver load balancer implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class SingleReceiverLoadBalancer<T> :
    IReceiverLoadBalancer<T>
    where T : class
{
    readonly IMessageReceiver<T> _receiver;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="receiver">The receiver value.</param>
    public SingleReceiverLoadBalancer(IMessageReceiver<T> receiver)
    {
        _receiver = receiver;
    }

    /// <summary>
    /// Performs the select receiver operation.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <returns>The result of the operation.</returns>
    public IMessageReceiver<T> SelectReceiver(T message)
    {
        return _receiver;
    }
}
