using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.RabbitMqTransport.Configuration;

public interface IRabbitMqBusConfiguration :
    IBusConfiguration
{
    new IRabbitMqHostConfiguration HostConfiguration { get; }

    new IRabbitMqEndpointConfiguration BusEndpointConfiguration { get; }

    new IRabbitMqTopologyConfiguration Topology { get; }

    IRabbitMqEndpointConfiguration CreateEndpointConfiguration(bool isBusEndpoint = false);
}
