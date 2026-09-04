namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>
/// Defines the contract for sql message send topology.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface ISqlMessageSendTopology<TMessage> :
    IMessageSendTopology<TMessage>
    where TMessage : class
{
}
