using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Defines the contract for rabbit mq host.
/// </summary>
public interface IRabbitMqHost :
    IHost<IRabbitMqReceiveEndpointConfigurator>
{
}
