using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.SqlTransport.Topology;

/// <summary>Defines the topology for sql message send.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class SqlMessageSendTopology<TMessage> :
    MessageSendTopology<TMessage>,
    ISqlMessageSendTopologyConfigurator<TMessage>
    where TMessage : class
{
}
