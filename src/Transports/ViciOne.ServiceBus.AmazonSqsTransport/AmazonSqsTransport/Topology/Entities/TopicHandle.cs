using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.AmazonSqsTransport.Topology;

public interface TopicHandle :
    EntityHandle
{
    Topic Topic { get; }
}
