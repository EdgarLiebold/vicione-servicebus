using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>
/// Defines the contract for service bus endpoint configuration.
/// </summary>
public interface IServiceBusEndpointConfiguration :
    IEndpointConfiguration
{
    /// <summary>
    /// Gets the topology value.
    /// </summary>
    new IServiceBusTopologyConfiguration Topology { get; }
}
