// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.SqlTransport
{
    using Topology;
    using Transports;


    public interface SqlReceiveEndpointContext :
        ReceiveEndpointContext
    {
        IClientContextSupervisor ClientContextSupervisor { get; }

        BrokerTopology BrokerTopology { get; }
    }
}
