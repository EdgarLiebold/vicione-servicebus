// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.RabbitMqTransport
{
    using Transports;


    public interface IRabbitMqHost :
        IHost<IRabbitMqReceiveEndpointConfigurator>
    {
    }
}
