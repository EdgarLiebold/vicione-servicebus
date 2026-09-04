using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.AzureServiceBusTransport.Topology;

public interface QueueHandle :
    EntityHandle
{
    Queue Queue { get; }
}
