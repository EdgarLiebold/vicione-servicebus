using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.AmazonSqsTransport.Topology;

public class AmazonSqsMessageSendTopology<TMessage> :
    MessageSendTopology<TMessage>,
    IAmazonSqsMessageSendTopologyConfigurator<TMessage>
    where TMessage : class
{
}
