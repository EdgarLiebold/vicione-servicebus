using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.SqlTransport.Configuration;

/// <summary>Stores and validates sql endpoint configuration.</summary>
public class SqlEndpointConfiguration :
    EndpointConfiguration,
    ISqlEndpointConfiguration
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="topologyConfiguration">The topology configuration.</param>
    public SqlEndpointConfiguration(ISqlTopologyConfiguration topologyConfiguration)
        : base(topologyConfiguration)
    {
        Topology = topologyConfiguration;
    }

    SqlEndpointConfiguration(IEndpointConfiguration parentConfiguration, ISqlTopologyConfiguration topologyConfiguration, bool isBusEndpoint)
        : base(parentConfiguration, topologyConfiguration, isBusEndpoint)
    {
        Topology = topologyConfiguration;
    }

    /// <summary>Gets the topology.</summary>
    public new ISqlTopologyConfiguration Topology { get; }

    /// <summary>Creates endpoint configuration.</summary>
    /// <param name="isBusEndpoint">The is bus endpoint.</param>
    /// <returns>The created endpoint configuration.</returns>
    public ISqlEndpointConfiguration CreateEndpointConfiguration(bool isBusEndpoint)
    {
        var topologyConfiguration = new SqlTopologyConfiguration(Topology);

        return new SqlEndpointConfiguration(this, topologyConfiguration, isBusEndpoint);
    }
}
