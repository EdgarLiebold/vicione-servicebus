namespace ViciOne.ServiceBus.AmazonSqsTransport.Topology;

using ViciOne.ServiceBus.Topology;


public interface TopicHandle :
    EntityHandle
{
    Topic Topic { get; }
}
