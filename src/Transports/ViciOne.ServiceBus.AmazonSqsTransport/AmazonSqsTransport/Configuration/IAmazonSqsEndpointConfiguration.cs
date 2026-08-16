namespace ViciOne.ServiceBus.AmazonSqsTransport.Configuration;

using ViciOne.ServiceBus.Configuration;


public interface IAmazonSqsEndpointConfiguration :
    IEndpointConfiguration
{
    new IAmazonSqsTopologyConfiguration Topology { get; }
}
