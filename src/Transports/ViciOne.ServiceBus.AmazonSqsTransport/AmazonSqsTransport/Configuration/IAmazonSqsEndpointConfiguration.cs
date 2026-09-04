using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.AmazonSqsTransport.Configuration;

public interface IAmazonSqsEndpointConfiguration :
    IEndpointConfiguration
{
    new IAmazonSqsTopologyConfiguration Topology { get; }
}
