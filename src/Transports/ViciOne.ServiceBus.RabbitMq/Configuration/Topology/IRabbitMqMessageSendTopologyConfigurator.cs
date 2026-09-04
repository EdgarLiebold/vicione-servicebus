namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Defines the contract for rabbit mq message send topology configurator.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IRabbitMqMessageSendTopologyConfigurator<TMessage> :
    IMessageSendTopologyConfigurator<TMessage>,
    IRabbitMqMessageSendTopology<TMessage>,
    IRabbitMqMessageSendTopologyConfigurator
    where TMessage : class
{
}


/// <summary>
/// Defines the contract for rabbit mq message send topology configurator.
/// </summary>
public interface IRabbitMqMessageSendTopologyConfigurator :
    IMessageSendTopologyConfigurator
{
}
