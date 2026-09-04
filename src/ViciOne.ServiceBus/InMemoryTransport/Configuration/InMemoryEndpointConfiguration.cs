using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.InMemoryTransport.Configuration;

/// <summary>
/// Provides an in memory endpoint configuration implementation.
/// </summary>
public class InMemoryEndpointConfiguration :
    EndpointConfiguration,
    IInMemoryEndpointConfiguration
{
    readonly IInMemoryTopologyConfiguration _topologyConfiguration;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="topologyConfiguration">The topology configuration value.</param>
    protected InMemoryEndpointConfiguration(IInMemoryTopologyConfiguration topologyConfiguration)
        : base(topologyConfiguration)
    {
        _topologyConfiguration = topologyConfiguration;
    }

    InMemoryEndpointConfiguration(IInMemoryEndpointConfiguration parentConfiguration, IInMemoryTopologyConfiguration topologyConfiguration,
        bool isBusEndpoint)
        : base(parentConfiguration, topologyConfiguration, isBusEndpoint)
    {
        _topologyConfiguration = topologyConfiguration;
    }

    IInMemoryTopologyConfiguration IInMemoryEndpointConfiguration.Topology => _topologyConfiguration;

    /// <summary>
    /// Creates endpoint configuration.
    /// </summary>
    /// <param name="isBusEndpoint">The is bus endpoint value.</param>
    /// <returns>The result of the operation.</returns>
    public IInMemoryEndpointConfiguration CreateEndpointConfiguration(bool isBusEndpoint)
    {
        var topologyConfiguration = new InMemoryTopologyConfiguration(_topologyConfiguration);

        return new InMemoryEndpointConfiguration(this, topologyConfiguration, isBusEndpoint);
    }
}
