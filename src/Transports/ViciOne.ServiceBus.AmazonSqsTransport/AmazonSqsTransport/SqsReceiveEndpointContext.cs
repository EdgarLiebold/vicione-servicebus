// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.AmazonSqsTransport;

using Topology;
using Transports;


public interface SqsReceiveEndpointContext :
    ReceiveEndpointContext
{
    BrokerTopology BrokerTopology { get; }

    IClientContextSupervisor ClientContextSupervisor { get; }
}
