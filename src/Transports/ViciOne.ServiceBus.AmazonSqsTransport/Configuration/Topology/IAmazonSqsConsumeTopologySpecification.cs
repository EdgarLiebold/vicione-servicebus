namespace ViciOne.ServiceBus;

using AmazonSqsTransport.Topology;


public interface IAmazonSqsConsumeTopologySpecification :
    ISpecification
{
    void Apply(IReceiveEndpointBrokerTopologyBuilder builder);
}
