namespace ViciOne.ServiceBus.ActiveMqTransport
{
    using Topology;
    using Transports;


    public interface ActiveMqReceiveEndpointContext :
        ReceiveEndpointContext
    {
        BrokerTopology BrokerTopology { get; }

        IConnectionContextSupervisor ConnectionContextSupervisor { get; }

        ISessionContextSupervisor SessionContextSupervisor { get; }
    }
}
