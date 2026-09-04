using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.AzureServiceBusTransport.Configuration;

public interface IServiceBusBusConfiguration :
    IBusConfiguration,
    IServiceBusEndpointConfiguration
{
    new IServiceBusHostConfiguration HostConfiguration { get; }

    new IServiceBusEndpointConfiguration BusEndpointConfiguration { get; }

    new IServiceBusTopologyConfiguration Topology { get; }

    IServiceBusEndpointConfiguration CreateEndpointConfiguration(bool isBusEndpoint = false);
}
