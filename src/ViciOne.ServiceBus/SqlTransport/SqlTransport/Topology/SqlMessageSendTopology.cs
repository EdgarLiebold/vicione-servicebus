// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
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
