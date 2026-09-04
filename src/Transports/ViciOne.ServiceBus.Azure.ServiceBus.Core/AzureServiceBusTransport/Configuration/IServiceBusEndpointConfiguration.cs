using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.AzureServiceBusTransport.Configuration;

public interface IServiceBusEndpointConfiguration :
    IEndpointConfiguration
{
    new IServiceBusTopologyConfiguration Topology { get; }
}
