using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.SqlTransport.Topology;

public class SqlMessageSendTopology<TMessage> :
    MessageSendTopology<TMessage>,
    ISqlMessageSendTopologyConfigurator<TMessage>
    where TMessage : class
{
}
