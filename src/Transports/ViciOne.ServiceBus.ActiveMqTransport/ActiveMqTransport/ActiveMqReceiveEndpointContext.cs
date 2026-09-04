using ViciOne.ServiceBus.ActiveMqTransport.Topology;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.ActiveMqTransport;

public interface ActiveMqReceiveEndpointContext :
    ReceiveEndpointContext
{
    BrokerTopology BrokerTopology { get; }

    IConnectionContextSupervisor ConnectionContextSupervisor { get; }

    ISessionContextSupervisor SessionContextSupervisor { get; }
}
