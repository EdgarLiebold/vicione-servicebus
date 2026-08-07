// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.RabbitMqTransport.Configuration
{
    using ViciOne.ServiceBus.Configuration;


    public interface IRabbitMqBusConfiguration :
        IBusConfiguration
    {
        new IRabbitMqHostConfiguration HostConfiguration { get; }

        new IRabbitMqEndpointConfiguration BusEndpointConfiguration { get; }

        new IRabbitMqTopologyConfiguration Topology { get; }

        IRabbitMqEndpointConfiguration CreateEndpointConfiguration(bool isBusEndpoint = false);
    }
}
