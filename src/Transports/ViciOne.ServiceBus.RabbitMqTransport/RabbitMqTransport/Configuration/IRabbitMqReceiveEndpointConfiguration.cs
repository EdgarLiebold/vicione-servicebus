using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMqTransport.Configuration;

public interface IRabbitMqReceiveEndpointConfiguration :
    IReceiveEndpointConfiguration,
    IRabbitMqEndpointConfiguration
{
    ReceiveSettings Settings { get; }

    void Build(IHost host);
}
