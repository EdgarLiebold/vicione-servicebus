using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.RabbitMqTransport.Topology;

public interface QueueHandle :
    EntityHandle
{
    Queue Queue { get; }
}
