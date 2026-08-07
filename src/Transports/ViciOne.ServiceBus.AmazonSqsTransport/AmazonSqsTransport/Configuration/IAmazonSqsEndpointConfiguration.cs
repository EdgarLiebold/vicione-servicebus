// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.AmazonSqsTransport.Configuration;

using ViciOne.ServiceBus.Configuration;


public interface IAmazonSqsEndpointConfiguration :
    IEndpointConfiguration
{
    new IAmazonSqsTopologyConfiguration Topology { get; }
}
