using ViciOne.ServiceBus.SqlTransport.Topology;

namespace ViciOne.ServiceBus.SqlTransport.Configuration;

public interface ISqlPublishTopologySpecification :
    ISpecification
{
    void Apply(IPublishEndpointBrokerTopologyBuilder builder);
}
