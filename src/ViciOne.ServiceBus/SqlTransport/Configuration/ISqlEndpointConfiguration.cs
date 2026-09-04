using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.SqlTransport.Configuration;

/// <summary>
/// Defines the contract for sql endpoint configuration.
/// </summary>
public interface ISqlEndpointConfiguration :
    IEndpointConfiguration
{
    /// <summary>
    /// Gets the topology value.
    /// </summary>
    new ISqlTopologyConfiguration Topology { get; }
}
