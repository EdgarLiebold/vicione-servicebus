using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>
/// Provides a service bus endpoint configuration implementation.
/// </summary>
public class ServiceBusEndpointConfiguration :
    EndpointConfiguration,
    IServiceBusEndpointConfiguration
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="topologyConfiguration">The topology configuration value.</param>
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

    /// <summary>
    /// Gets the topology value.
    /// </summary>
    public new IServiceBusTopologyConfiguration Topology { get; }

    /// <summary>
    /// Creates endpoint configuration.
    /// </summary>
    /// <param name="isBusEndpoint">The is bus endpoint value.</param>
    /// <returns>The result of the operation.</returns>
    public IServiceBusEndpointConfiguration CreateEndpointConfiguration(bool isBusEndpoint)
    {
        var topologyConfiguration = new ServiceBusTopologyConfiguration(Topology);

        return new ServiceBusEndpointConfiguration(this, topologyConfiguration, isBusEndpoint);
    }
}
