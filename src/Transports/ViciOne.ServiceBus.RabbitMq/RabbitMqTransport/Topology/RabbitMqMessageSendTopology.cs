using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>
/// Provides a rabbit mq message send topology implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class RabbitMqMessageSendTopology<TMessage> :
    MessageSendTopology<TMessage>,
    IRabbitMqMessageSendTopologyConfigurator<TMessage>
    where TMessage : class
{
}
