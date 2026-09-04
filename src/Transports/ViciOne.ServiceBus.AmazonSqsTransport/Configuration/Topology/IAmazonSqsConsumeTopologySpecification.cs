using ViciOne.ServiceBus.AmazonSqsTransport.Topology;

namespace ViciOne.ServiceBus;

public interface IAmazonSqsConsumeTopologySpecification :
    ISpecification
{
    void Apply(IReceiveEndpointBrokerTopologyBuilder builder);
}
