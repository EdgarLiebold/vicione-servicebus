namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>Balances work across single receiver instances.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class SingleReceiverLoadBalancer<T> :
    IReceiverLoadBalancer<T>
    where T : class
{
    readonly IMessageReceiver<T> _receiver;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="receiver">The receiver.</param>
    public SingleReceiverLoadBalancer(IMessageReceiver<T> receiver)
    {
        _receiver = receiver;
    }

    /// <summary>Selects receiver.</summary>
    /// <param name="message">The message to process.</param>
    /// <returns>The selected receiver.</returns>
    public IMessageReceiver<T> SelectReceiver(T message)
    {
        return _receiver;
    }
}
