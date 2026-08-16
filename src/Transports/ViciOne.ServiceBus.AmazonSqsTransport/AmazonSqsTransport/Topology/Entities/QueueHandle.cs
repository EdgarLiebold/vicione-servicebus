namespace ViciOne.ServiceBus.AmazonSqsTransport.Topology;

using ViciOne.ServiceBus.Topology;


public interface QueueHandle :
    EntityHandle
{
    Queue Queue { get; }
}
