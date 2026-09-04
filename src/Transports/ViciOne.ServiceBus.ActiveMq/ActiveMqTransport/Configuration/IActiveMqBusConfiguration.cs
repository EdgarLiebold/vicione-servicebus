using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.ActiveMq.Configuration;

/// <summary>
/// Defines the contract for active mq bus configuration.
/// </summary>
public interface IActiveMqBusConfiguration :
    IBusConfiguration
{
    /// <summary>
    /// Gets the host configuration value.
    /// </summary>
    new IActiveMqHostConfiguration HostConfiguration { get; }

    /// <summary>
    /// Gets the bus endpoint configuration value.
    /// </summary>
    new IActiveMqEndpointConfiguration BusEndpointConfiguration { get; }

    /// <summary>
    /// Gets the topology value.
    /// </summary>
    new IActiveMqTopologyConfiguration Topology { get; }

    /// <summary>
    /// Create an endpoint configuration on the bus, which can later be turned into a receive endpoint
    /// </summary>
    /// <returns></returns>
    IActiveMqEndpointConfiguration CreateEndpointConfiguration(bool isBusEndpoint = false);
}
