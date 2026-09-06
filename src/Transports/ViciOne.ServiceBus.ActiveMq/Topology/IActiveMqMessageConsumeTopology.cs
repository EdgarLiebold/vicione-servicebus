namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Exposes ActiveMQ consume topology for one message type.</summary>
/// <typeparam name="TMessage">The consumed message type.</typeparam>
public interface IActiveMqMessageConsumeTopology<TMessage> :
    IMessageConsumeTopology<TMessage>
    where TMessage : class
{
}
