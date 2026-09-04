using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.RabbitMqTransport.Topology;

public interface ExchangeBindingHandle :
    EntityHandle
{
    ExchangeToExchangeBinding Binding { get; }
}
