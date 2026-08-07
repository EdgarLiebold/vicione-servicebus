// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.RabbitMqTransport.Configuration
{
    using ViciOne.ServiceBus.Configuration;


    public interface IRabbitMqEndpointConfiguration :
        IEndpointConfiguration
    {
        new IRabbitMqTopologyConfiguration Topology { get; }
    }
}
