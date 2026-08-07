// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.AzureServiceBusTransport.Configuration
{
    using ViciOne.ServiceBus.Configuration;


    public interface IServiceBusBusConfiguration :
        IBusConfiguration,
        IServiceBusEndpointConfiguration
    {
        new IServiceBusHostConfiguration HostConfiguration { get; }

        new IServiceBusEndpointConfiguration BusEndpointConfiguration { get; }

        new IServiceBusTopologyConfiguration Topology { get; }

        IServiceBusEndpointConfiguration CreateEndpointConfiguration(bool isBusEndpoint = false);
    }
}
