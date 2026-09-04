using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.ActiveMqTransport.Topology;

public interface TopicHandle :
    EntityHandle
{
    Topic Topic { get; }
}
