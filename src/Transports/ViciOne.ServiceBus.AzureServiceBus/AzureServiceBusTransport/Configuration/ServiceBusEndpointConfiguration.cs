using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>Adds Azure Service Bus topology configuration to endpoint configuration.</summary>
public class ServiceBusEndpointConfiguration :
    EndpointConfiguration,
    IServiceBusEndpointConfiguration
{
    /// <summary>Initializes the root endpoint configuration.</summary>
    /// <param name="topologyConfiguration">The transport topology configuration owned by the endpoint.</param>
    protected ServiceBusEndpointConfiguration(IServiceBusTopologyConfiguration topologyConfiguration)
        : base(topologyConfiguration)
    {
        Topology = topologyConfiguration;
    }

    ServiceBusEndpointConfiguration(IServiceBusEndpointConfiguration parentConfiguration, IServiceBusTopologyConfiguration topologyConfiguration,
        bool isBusEndpoint)
        : base(parentConfiguration, topologyConfiguration, isBusEndpoint)
    {
        Topology = topologyConfiguration;
    }

    /// <summary>Gets the endpoint's Azure Service Bus topology configuration.</summary>
    public new IServiceBusTopologyConfiguration Topology { get; }

    /// <summary>Creates a child endpoint configuration with separate consume topology and shared message, send, and publish topology.</summary>
    /// <param name="isBusEndpoint">Whether the child configures the bus endpoint.</param>
    /// <returns>The child endpoint configuration.</returns>
    public IServiceBusEndpointConfiguration CreateEndpointConfiguration(bool isBusEndpoint)
    {
        var topologyConfiguration = new ServiceBusTopologyConfiguration(Topology);

        return new ServiceBusEndpointConfiguration(this, topologyConfiguration, isBusEndpoint);
    }
}
