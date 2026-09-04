using ViciOne.ServiceBus.SqlTransport.Topology;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.SqlTransport;

public interface SqlReceiveEndpointContext :
    ReceiveEndpointContext
{
    IClientContextSupervisor ClientContextSupervisor { get; }

    BrokerTopology BrokerTopology { get; }
}
