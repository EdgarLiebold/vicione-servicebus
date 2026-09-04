using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.AzureServiceBusTransport.Topology;

public interface TopicHandle :
    EntityHandle
{
    Topic Topic { get; }
}
