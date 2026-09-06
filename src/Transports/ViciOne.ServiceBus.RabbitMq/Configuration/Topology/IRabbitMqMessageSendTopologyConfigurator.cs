namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Configures RabbitMQ send topology for one message contract.</summary>
/// <typeparam name="TMessage">The message contract.</typeparam>
public interface IRabbitMqMessageSendTopologyConfigurator<TMessage> :
    IMessageSendTopologyConfigurator<TMessage>,
    IRabbitMqMessageSendTopology<TMessage>,
    IRabbitMqMessageSendTopologyConfigurator
    where TMessage : class
{
}


/// <summary>Configures RabbitMQ send-topology conventions for one message contract.</summary>
public interface IRabbitMqMessageSendTopologyConfigurator :
    IMessageSendTopologyConfigurator
{
}
