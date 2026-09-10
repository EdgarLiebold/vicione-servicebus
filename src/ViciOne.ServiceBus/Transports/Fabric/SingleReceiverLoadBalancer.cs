namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>Always selects the sole connected receiver.</summary>
/// <typeparam name="TMessage">The message type accepted by the receiver.</typeparam>
internal sealed class SingleReceiverLoadBalancer<TMessage> :
    IReceiverLoadBalancer<TMessage>
    where TMessage : class
{
    readonly IMessageReceiver<TMessage> _receiver;

    /// <summary>Initializes a load balancer for one receiver.</summary>
    /// <param name="receiver">The receiver selected for every message.</param>
    public SingleReceiverLoadBalancer(IMessageReceiver<TMessage> receiver)
    {
        ArgumentNullException.ThrowIfNull(receiver);
        _receiver = receiver;
    }

    /// <inheritdoc />
    public IMessageReceiver<TMessage> SelectReceiver(TMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return _receiver;
    }
}
