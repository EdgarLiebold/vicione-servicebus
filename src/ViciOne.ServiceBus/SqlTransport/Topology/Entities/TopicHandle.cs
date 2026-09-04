using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.SqlTransport.Topology;

public interface TopicHandle :
    EntityHandle
{
    Topic Topic { get; }
}
