using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.SqlTransport.Configuration;

/// <summary>
/// Provides a sql endpoint configuration implementation.
/// </summary>
public class SqlEndpointConfiguration :
    EndpointConfiguration,
    ISqlEndpointConfiguration
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="topologyConfiguration">The topology configuration value.</param>
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

    /// <summary>
    /// Gets the topology value.
    /// </summary>
    public new ISqlTopologyConfiguration Topology { get; }

    /// <summary>
    /// Creates endpoint configuration.
    /// </summary>
    /// <param name="isBusEndpoint">The is bus endpoint value.</param>
    /// <returns>The result of the operation.</returns>
    public ISqlEndpointConfiguration CreateEndpointConfiguration(bool isBusEndpoint)
    {
        var topologyConfiguration = new SqlTopologyConfiguration(Topology);

        return new SqlEndpointConfiguration(this, topologyConfiguration, isBusEndpoint);
    }
}
