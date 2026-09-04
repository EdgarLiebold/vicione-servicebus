using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>
/// Provides a rabbit mq endpoint configuration implementation.
/// </summary>
public class RabbitMqEndpointConfiguration :
    EndpointConfiguration,
    IRabbitMqEndpointConfiguration
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="topologyConfiguration">The topology configuration value.</param>
    public RabbitMqEndpointConfiguration(IRabbitMqTopologyConfiguration topologyConfiguration)
        : base(topologyConfiguration)
    {
        Topology = topologyConfiguration;
    }

    RabbitMqEndpointConfiguration(IEndpointConfiguration parentConfiguration, IRabbitMqTopologyConfiguration topologyConfiguration, bool isBusEndpoint)
        : base(parentConfiguration, topologyConfiguration, isBusEndpoint)
    {
        Topology = topologyConfiguration;
    }

    /// <summary>
    /// Gets the topology value.
    /// </summary>
    public new IRabbitMqTopologyConfiguration Topology { get; }

    /// <summary>
    /// Creates endpoint configuration.
    /// </summary>
    /// <param name="isBusEndpoint">The is bus endpoint value.</param>
    /// <returns>The result of the operation.</returns>
    public IRabbitMqEndpointConfiguration CreateEndpointConfiguration(bool isBusEndpoint)
    {
        var topologyConfiguration = new RabbitMqTopologyConfiguration(Topology);

        return new RabbitMqEndpointConfiguration(this, topologyConfiguration, isBusEndpoint);
    }
}
