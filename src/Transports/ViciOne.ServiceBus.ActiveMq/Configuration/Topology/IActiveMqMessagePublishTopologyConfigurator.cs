namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Configures the ActiveMQ publish topic for one message type.</summary>
/// <typeparam name="TMessage">The published message type.</typeparam>
public interface IActiveMqMessagePublishTopologyConfigurator<TMessage> :
    IMessagePublishTopologyConfigurator<TMessage>,
    IActiveMqMessagePublishTopology<TMessage>,
    IActiveMqMessagePublishTopologyConfigurator
    where TMessage : class
{
}


/// <summary>Configures an untyped ActiveMQ message publish topic.</summary>
public interface IActiveMqMessagePublishTopologyConfigurator :
    IMessagePublishTopologyConfigurator,
    IActiveMqMessagePublishTopology,
    IActiveMqTopicConfigurator
{
}
