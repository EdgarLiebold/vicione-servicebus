using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.ActiveMq.Configuration;

/// <summary>
/// Provides an active mq endpoint configuration implementation.
/// </summary>
public class ActiveMqEndpointConfiguration :
    EndpointConfiguration,
    IActiveMqEndpointConfiguration
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="topologyConfiguration">The topology configuration value.</param>
    protected ActiveMqEndpointConfiguration(IActiveMqTopologyConfiguration topologyConfiguration)
        : base(topologyConfiguration)
    {
        Topology = topologyConfiguration;
    }

    ActiveMqEndpointConfiguration(IEndpointConfiguration parentConfiguration, IActiveMqTopologyConfiguration topologyConfiguration, bool isBusEndpoint)
        : base(parentConfiguration, topologyConfiguration, isBusEndpoint)
    {
        Topology = topologyConfiguration;
    }

    /// <summary>
    /// Gets the topology value.
    /// </summary>
    public new IActiveMqTopologyConfiguration Topology { get; }

    /// <summary>
    /// Creates endpoint configuration.
    /// </summary>
    /// <param name="isBusEndpoint">The is bus endpoint value.</param>
    /// <returns>The result of the operation.</returns>
    public IActiveMqEndpointConfiguration CreateEndpointConfiguration(bool isBusEndpoint)
    {
        var topologyConfiguration = new ActiveMqTopologyConfiguration(Topology);

        return new ActiveMqEndpointConfiguration(this, topologyConfiguration, isBusEndpoint);
    }
}
