using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.SqlTransport.Topology;

/// <summary>
/// Provides a sql message send topology implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class SqlMessageSendTopology<TMessage> :
    MessageSendTopology<TMessage>,
    ISqlMessageSendTopologyConfigurator<TMessage>
    where TMessage : class
{
}
