namespace ViciOne.ServiceBus.AmazonSqsTransport.Topology;

using ViciOne.ServiceBus.Topology;


public class AmazonSqsMessageSendTopology<TMessage> :
    MessageSendTopology<TMessage>,
    IAmazonSqsMessageSendTopologyConfigurator<TMessage>
    where TMessage : class
{
}
