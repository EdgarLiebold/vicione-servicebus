using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>Stores provider-neutral endpoint settings together with RabbitMQ topology configuration.</summary>
public class RabbitMqEndpointConfiguration :
    EndpointConfiguration,
    IRabbitMqEndpointConfiguration
{
    /// <summary>Creates a root endpoint configuration.</summary>
    /// <param name="topologyConfiguration">The RabbitMQ topology configuration.</param>
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

    /// <summary>Gets the RabbitMQ topology configuration scoped to this endpoint.</summary>
    public new IRabbitMqTopologyConfiguration Topology { get; }

    /// <summary>Creates a child endpoint configuration with new consume topology and retained parent message, send, and publish topology references.</summary>
    /// <param name="isBusEndpoint">Whether the child belongs to the bus endpoint.</param>
    /// <returns>The new child endpoint configuration.</returns>
    public IRabbitMqEndpointConfiguration CreateEndpointConfiguration(bool isBusEndpoint)
    {
        var topologyConfiguration = new RabbitMqTopologyConfiguration(Topology);

        return new RabbitMqEndpointConfiguration(this, topologyConfiguration, isBusEndpoint);
    }
}
