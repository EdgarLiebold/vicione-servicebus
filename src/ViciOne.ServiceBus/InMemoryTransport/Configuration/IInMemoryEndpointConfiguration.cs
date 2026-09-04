using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.InMemoryTransport.Configuration;

public interface IInMemoryEndpointConfiguration :
    IEndpointConfiguration
{
    new IInMemoryTopologyConfiguration Topology { get; }
}
