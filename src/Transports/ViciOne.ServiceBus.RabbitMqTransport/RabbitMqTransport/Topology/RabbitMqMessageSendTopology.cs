using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.RabbitMqTransport.Topology;

public class RabbitMqMessageSendTopology<TMessage> :
    MessageSendTopology<TMessage>,
    IRabbitMqMessageSendTopologyConfigurator<TMessage>
    where TMessage : class
{
}
