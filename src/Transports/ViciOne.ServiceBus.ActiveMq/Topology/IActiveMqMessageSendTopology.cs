namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Exposes ActiveMQ send topology for one message type.</summary>
/// <typeparam name="TMessage">The sent message type.</typeparam>
public interface IActiveMqMessageSendTopology<TMessage> :
    IMessageSendTopology<TMessage>
    where TMessage : class
{
}
