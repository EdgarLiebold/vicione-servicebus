using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMqTransport;

public interface IRabbitMqHost :
    IHost<IRabbitMqReceiveEndpointConfigurator>
{
}
