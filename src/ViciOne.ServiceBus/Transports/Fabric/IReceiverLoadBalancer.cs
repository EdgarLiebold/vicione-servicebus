namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>Selects a connected receiver for each queued message.</summary>
/// <typeparam name="TMessage">The message type.</typeparam>
internal interface IReceiverLoadBalancer<in TMessage>
    where TMessage : class
{
    /// <summary>Selects the receiver for a message.</summary>
    /// <param name="message">The message being dispatched.</param>
    /// <returns>The selected receiver.</returns>
    IMessageReceiver<TMessage> SelectReceiver(TMessage message);
}
