using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.AzureServiceBusTransport.Topology;

public class ServiceBusMessageSendTopology<TMessage> :
    MessageSendTopology<TMessage>,
    IServiceBusMessageSendTopologyConfigurator<TMessage>
    where TMessage : class
{
}
