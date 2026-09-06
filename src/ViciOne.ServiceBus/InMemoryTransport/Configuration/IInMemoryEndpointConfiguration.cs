using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.InMemoryTransport.Configuration;

/// <summary>Defines in memory endpoint configuration.</summary>
public interface IInMemoryEndpointConfiguration :
    IEndpointConfiguration
{
    /// <summary>Gets the topology.</summary>
    new IInMemoryTopologyConfiguration Topology { get; }
}
