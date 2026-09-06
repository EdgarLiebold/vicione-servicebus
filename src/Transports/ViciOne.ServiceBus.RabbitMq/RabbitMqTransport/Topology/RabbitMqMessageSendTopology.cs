using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>Provides the provider-specific send-topology marker for a RabbitMQ message contract.</summary>
/// <typeparam name="TMessage">The sent message contract type.</typeparam>
public class RabbitMqMessageSendTopology<TMessage> :
    MessageSendTopology<TMessage>,
    IRabbitMqMessageSendTopologyConfigurator<TMessage>
    where TMessage : class
{
}
