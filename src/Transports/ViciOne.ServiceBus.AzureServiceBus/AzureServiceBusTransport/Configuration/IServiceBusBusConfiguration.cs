using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>
/// Defines the contract for service bus bus configuration.
/// </summary>
public interface IServiceBusBusConfiguration :
    IBusConfiguration,
    IServiceBusEndpointConfiguration
{
    /// <summary>
    /// Gets the host configuration value.
    /// </summary>
    new IServiceBusHostConfiguration HostConfiguration { get; }

    /// <summary>
    /// Gets the bus endpoint configuration value.
    /// </summary>
    new IServiceBusEndpointConfiguration BusEndpointConfiguration { get; }

    /// <summary>
    /// Gets the topology value.
    /// </summary>
    new IServiceBusTopologyConfiguration Topology { get; }

    /// <summary>
    /// Creates endpoint configuration.
    /// </summary>
    /// <param name="isBusEndpoint">The is bus endpoint value.</param>
    /// <returns>The result of the operation.</returns>
    IServiceBusEndpointConfiguration CreateEndpointConfiguration(bool isBusEndpoint = false);
}
