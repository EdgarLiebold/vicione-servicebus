using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.ActiveMq.Configuration;

/// <summary>Owns transport-independent and ActiveMQ topology configuration for an endpoint.</summary>
public class ActiveMqEndpointConfiguration :
    EndpointConfiguration,
    IActiveMqEndpointConfiguration
{
    /// <summary>Creates a root ActiveMQ endpoint configuration.</summary>
    /// <param name="topologyConfiguration">The ActiveMQ topology configuration.</param>
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

    /// <summary>Gets the ActiveMQ topology configuration.</summary>
    public new IActiveMqTopologyConfiguration Topology { get; }

    /// <summary>Creates a child endpoint configuration with a copied topology configuration.</summary>
    /// <param name="isBusEndpoint">Whether the child configures the bus endpoint.</param>
    /// <returns>The child ActiveMQ endpoint configuration.</returns>
    public IActiveMqEndpointConfiguration CreateEndpointConfiguration(bool isBusEndpoint)
    {
        var topologyConfiguration = new ActiveMqTopologyConfiguration(Topology);

        return new ActiveMqEndpointConfiguration(this, topologyConfiguration, isBusEndpoint);
    }
}
