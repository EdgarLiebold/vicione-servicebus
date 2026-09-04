using ViciOne.ServiceBus.SqlTransport.Topology;

namespace ViciOne.ServiceBus.SqlTransport.Configuration;

public interface ISqlConsumeTopologySpecification :
    ISpecification
{
    void Apply(IReceiveEndpointBrokerTopologyBuilder builder);
}
