using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.InMemoryTransport.Configuration;

/// <summary>Exposes the topology associated with an in-memory endpoint configuration.</summary>
internal interface IInMemoryEndpointConfiguration :
    IEndpointConfiguration
{
    /// <summary>Gets the endpoint's in-memory topology.</summary>
    new IInMemoryTopologyConfiguration Topology { get; }
}
