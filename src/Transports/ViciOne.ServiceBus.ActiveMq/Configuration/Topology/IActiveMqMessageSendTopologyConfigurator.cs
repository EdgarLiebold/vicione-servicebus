namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>
/// Defines the contract for active mq message send topology configurator.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IActiveMqMessageSendTopologyConfigurator<TMessage> :
    IMessageSendTopologyConfigurator<TMessage>,
    IActiveMqMessageSendTopology<TMessage>,
    IActiveMqMessageSendTopologyConfigurator
    where TMessage : class
{
}


/// <summary>
/// Defines the contract for active mq message send topology configurator.
/// </summary>
public interface IActiveMqMessageSendTopologyConfigurator :
    IMessageSendTopologyConfigurator
{
}
