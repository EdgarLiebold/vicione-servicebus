using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.InMemoryTransport.Configuration;

/// <summary>Owns transport-independent pipelines and in-memory topology for an endpoint scope.</summary>
internal class InMemoryEndpointConfiguration :
    EndpointConfiguration,
    IInMemoryEndpointConfiguration
{
    readonly IInMemoryTopologyConfiguration _topologyConfiguration;

    /// <summary>Creates a root endpoint configuration over the supplied topology.</summary>
    /// <param name="topologyConfiguration">The in-memory topology owned by the endpoint scope.</param>
    protected InMemoryEndpointConfiguration(IInMemoryTopologyConfiguration topologyConfiguration)
        : base(topologyConfiguration ?? throw new ArgumentNullException(nameof(topologyConfiguration)))
    {
        _topologyConfiguration = topologyConfiguration;
    }

    InMemoryEndpointConfiguration(IInMemoryEndpointConfiguration parentConfiguration, IInMemoryTopologyConfiguration topologyConfiguration,
        bool isBusEndpoint)
        : base(
            parentConfiguration ?? throw new ArgumentNullException(nameof(parentConfiguration)),
            topologyConfiguration ?? throw new ArgumentNullException(nameof(topologyConfiguration)),
            isBusEndpoint)
    {
        _topologyConfiguration = topologyConfiguration;
    }

    IInMemoryTopologyConfiguration IInMemoryEndpointConfiguration.Topology => _topologyConfiguration;

    /// <summary>Creates a child endpoint configuration that inherits this scope's pipelines.</summary>
    /// <param name="isBusEndpoint">Whether the child represents the bus endpoint.</param>
    /// <returns>An independently materialized child configuration.</returns>
    public IInMemoryEndpointConfiguration CreateEndpointConfiguration(bool isBusEndpoint)
    {
        return CreateChildConfiguration(this, isBusEndpoint);
    }

    /// <summary>Creates an endpoint configuration that inherits another in-memory endpoint's pipelines.</summary>
    /// <param name="parentConfiguration">The endpoint configuration whose pipeline specifications are inherited.</param>
    /// <param name="isBusEndpoint">Whether the child represents a bus endpoint.</param>
    /// <returns>A child configuration with independent receive-pipe materialization.</returns>
    internal static IInMemoryEndpointConfiguration CreateChildConfiguration(
        IInMemoryEndpointConfiguration parentConfiguration,
        bool isBusEndpoint = false)
    {
        ArgumentNullException.ThrowIfNull(parentConfiguration);

        var topologyConfiguration = new InMemoryTopologyConfiguration(parentConfiguration.Topology);
        return new InMemoryEndpointConfiguration(parentConfiguration, topologyConfiguration, isBusEndpoint);
    }
}
