using ViciOne.ServiceBus.AmazonSqsTransport.Topology;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqsTransport;

public interface SqsReceiveEndpointContext :
    ReceiveEndpointContext
{
    BrokerTopology BrokerTopology { get; }

    IClientContextSupervisor ClientContextSupervisor { get; }
}
