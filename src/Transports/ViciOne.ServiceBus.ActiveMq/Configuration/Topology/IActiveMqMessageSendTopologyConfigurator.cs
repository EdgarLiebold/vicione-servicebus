namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Configures ActiveMQ send topology for one message type.</summary>
/// <typeparam name="TMessage">The sent message type.</typeparam>
public interface IActiveMqMessageSendTopologyConfigurator<TMessage> :
    IMessageSendTopologyConfigurator<TMessage>,
    IActiveMqMessageSendTopology<TMessage>,
    IActiveMqMessageSendTopologyConfigurator
    where TMessage : class
{
}


/// <summary>Configures untyped ActiveMQ message send topology.</summary>
public interface IActiveMqMessageSendTopologyConfigurator :
    IMessageSendTopologyConfigurator
{
}
