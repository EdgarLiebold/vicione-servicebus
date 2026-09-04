using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>
/// Defines the contract for rabbit mq bus configuration.
/// </summary>
public interface IRabbitMqBusConfiguration :
    IBusConfiguration
{
    /// <summary>
    /// Gets the host configuration value.
    /// </summary>
    new IRabbitMqHostConfiguration HostConfiguration { get; }

    /// <summary>
    /// Gets the bus endpoint configuration value.
    /// </summary>
    new IRabbitMqEndpointConfiguration BusEndpointConfiguration { get; }

    /// <summary>
    /// Gets the topology value.
    /// </summary>
    new IRabbitMqTopologyConfiguration Topology { get; }

    /// <summary>
    /// Creates endpoint configuration.
    /// </summary>
    /// <param name="isBusEndpoint">The is bus endpoint value.</param>
    /// <returns>The result of the operation.</returns>
    IRabbitMqEndpointConfiguration CreateEndpointConfiguration(bool isBusEndpoint = false);
}
