namespace ViciOne.ServiceBus.ActiveMqTransport
{
    using Topology;
    using Transports;


    public interface ActiveMqReceiveEndpointContext :
        ReceiveEndpointContext
    {
        BrokerTopology BrokerTopology { get; }

        ISessionContextSupervisor SessionContextSupervisor { get; }
    }
}
