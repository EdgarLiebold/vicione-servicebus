using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.RabbitMqTransport.Topology;

public interface ExchangeHandle :
    EntityHandle
{
    Exchange Exchange { get; }
}
