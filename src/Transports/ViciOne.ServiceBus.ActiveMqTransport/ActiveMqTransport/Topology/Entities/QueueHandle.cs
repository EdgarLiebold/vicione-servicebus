using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.ActiveMqTransport.Topology;

public interface QueueHandle :
    EntityHandle
{
    Queue Queue { get; }
}
