namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>
/// Defines the contract for active mq message publish topology configurator.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IActiveMqMessagePublishTopologyConfigurator<TMessage> :
    IMessagePublishTopologyConfigurator<TMessage>,
    IActiveMqMessagePublishTopology<TMessage>,
    IActiveMqMessagePublishTopologyConfigurator
    where TMessage : class
{
}


/// <summary>
/// Defines the contract for active mq message publish topology configurator.
/// </summary>
public interface IActiveMqMessagePublishTopologyConfigurator :
    IMessagePublishTopologyConfigurator,
    IActiveMqMessagePublishTopology,
    IActiveMqTopicConfigurator
{
}
