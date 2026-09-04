using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.SqlTransport.Configuration;

/// <summary>
/// Defines the contract for sql bus configuration.
/// </summary>
public interface ISqlBusConfiguration :
    IBusConfiguration
{
    /// <summary>
    /// Gets the host configuration value.
    /// </summary>
    new ISqlHostConfiguration HostConfiguration { get; }

    /// <summary>
    /// Gets the bus endpoint configuration value.
    /// </summary>
    new ISqlEndpointConfiguration BusEndpointConfiguration { get; }

    /// <summary>
    /// Gets the topology value.
    /// </summary>
    new ISqlTopologyConfiguration Topology { get; }

    /// <summary>
    /// Creates endpoint configuration.
    /// </summary>
    /// <param name="isBusEndpoint">The is bus endpoint value.</param>
    /// <returns>The result of the operation.</returns>
    ISqlEndpointConfiguration CreateEndpointConfiguration(bool isBusEndpoint = false);
}
