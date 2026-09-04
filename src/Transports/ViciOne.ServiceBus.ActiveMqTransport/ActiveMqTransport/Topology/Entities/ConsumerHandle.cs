using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.ActiveMqTransport.Topology;

public interface ConsumerHandle :
    EntityHandle
{
    Consumer Consumer { get; }
}
