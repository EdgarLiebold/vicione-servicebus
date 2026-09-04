using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.SqlTransport.Configuration;

public interface ISqlEndpointConfiguration :
    IEndpointConfiguration
{
    new ISqlTopologyConfiguration Topology { get; }
}
