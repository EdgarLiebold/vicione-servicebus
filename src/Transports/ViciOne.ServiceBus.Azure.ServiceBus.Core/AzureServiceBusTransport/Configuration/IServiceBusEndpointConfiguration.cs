// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.AzureServiceBusTransport.Configuration
{
    using ViciOne.ServiceBus.Configuration;


    public interface IServiceBusEndpointConfiguration :
        IEndpointConfiguration
    {
        new IServiceBusTopologyConfiguration Topology { get; }
    }
}
