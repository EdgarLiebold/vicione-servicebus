namespace ViciOne.ServiceBus.ActiveMqTransport.Topology
{
    using ViciOne.ServiceBus.Topology;


    public class ActiveMqMessageSendTopology<TMessage> :
        MessageSendTopology<TMessage>,
        IActiveMqMessageSendTopologyConfigurator<TMessage>
        where TMessage : class
    {
    }
}
