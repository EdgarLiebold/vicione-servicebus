using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>Provides ActiveMQ send-topology configuration for one message type.</summary>
/// <typeparam name="TMessage">The sent message type.</typeparam>
public class ActiveMqMessageSendTopology<TMessage> :
    MessageSendTopology<TMessage>,
    IActiveMqMessageSendTopologyConfigurator<TMessage>
    where TMessage : class
{
}
