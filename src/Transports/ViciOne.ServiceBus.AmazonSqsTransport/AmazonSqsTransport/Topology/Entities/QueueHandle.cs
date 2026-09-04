using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.AmazonSqsTransport.Topology;

public interface QueueHandle :
    EntityHandle
{
    Queue Queue { get; }
}
