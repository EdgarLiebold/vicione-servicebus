using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.RabbitMqTransport.Topology;

public interface QueueBindingHandle :
    EntityHandle
{
    ExchangeToQueueBinding Binding { get; }
}
