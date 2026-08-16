namespace ViciOne.ServiceBus.SqlTransport.Topology
{
    using ViciOne.ServiceBus.Topology;


    public class SqlMessageSendTopology<TMessage> :
        MessageSendTopology<TMessage>,
        ISqlMessageSendTopologyConfigurator<TMessage>
        where TMessage : class
    {
    }
}
