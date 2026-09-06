using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.SqlTransport.Configuration;

/// <summary>Defines sql endpoint configuration.</summary>
public interface ISqlEndpointConfiguration :
    IEndpointConfiguration
{
    /// <summary>Gets the topology.</summary>
    new ISqlTopologyConfiguration Topology { get; }
}
