using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>
/// Provides an active mq message send topology implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class ActiveMqMessageSendTopology<TMessage> :
    MessageSendTopology<TMessage>,
    IActiveMqMessageSendTopologyConfigurator<TMessage>
    where TMessage : class
{
}
