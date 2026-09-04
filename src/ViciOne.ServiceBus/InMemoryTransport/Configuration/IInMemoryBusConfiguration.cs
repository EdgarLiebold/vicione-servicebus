using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.InMemoryTransport.Configuration;

/// <summary>
/// Defines the contract for in memory bus configuration.
/// </summary>
public interface IInMemoryBusConfiguration :
    IBusConfiguration,
    IInMemoryEndpointConfiguration
{
    /// <summary>
    /// Gets the host configuration value.
    /// </summary>
    new IInMemoryHostConfiguration HostConfiguration { get; }

    /// <summary>
    /// Gets the bus endpoint configuration value.
    /// </summary>
    new IInMemoryEndpointConfiguration BusEndpointConfiguration { get; }

    /// <summary>
    /// Create an endpoint configuration on the bus, which can later be turned into a receive endpoint
    /// </summary>
    /// <returns></returns>
    IInMemoryEndpointConfiguration CreateEndpointConfiguration(bool isBusEndpoint = false);
}
