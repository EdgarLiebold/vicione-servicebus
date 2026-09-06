using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Hosts RabbitMQ receive endpoints.</summary>
public interface IRabbitMqHost :
    IHost<IRabbitMqReceiveEndpointConfigurator>
{
}
