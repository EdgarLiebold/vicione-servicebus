namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>
/// Defines the contract for active mq message send topology.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IActiveMqMessageSendTopology<TMessage> :
    IMessageSendTopology<TMessage>
    where TMessage : class
{
}
