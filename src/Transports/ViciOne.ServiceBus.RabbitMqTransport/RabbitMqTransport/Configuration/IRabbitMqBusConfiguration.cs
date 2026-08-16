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
