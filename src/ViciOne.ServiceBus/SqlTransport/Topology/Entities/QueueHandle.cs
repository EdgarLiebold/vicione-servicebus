using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.SqlTransport.Topology;

public interface QueueHandle :
    EntityHandle
{
    Queue Queue { get; }
}
