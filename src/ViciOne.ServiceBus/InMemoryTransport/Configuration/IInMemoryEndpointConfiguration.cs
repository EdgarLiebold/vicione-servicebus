using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.InMemoryTransport.Configuration;

/// <summary>
/// Defines the contract for in memory endpoint configuration.
/// </summary>
public interface IInMemoryEndpointConfiguration :
    IEndpointConfiguration
{
    /// <summary>
    /// Gets the topology value.
    /// </summary>
    new IInMemoryTopologyConfiguration Topology { get; }
}
