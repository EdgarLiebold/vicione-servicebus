using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.ActiveMqTransport.Topology;

public class ActiveMqMessageSendTopology<TMessage> :
    MessageSendTopology<TMessage>,
    IActiveMqMessageSendTopologyConfigurator<TMessage>
    where TMessage : class
{
}
