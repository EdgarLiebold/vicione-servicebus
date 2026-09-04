namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>
/// Defines the contract for active mq message consume topology.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IActiveMqMessageConsumeTopology<TMessage> :
    IMessageConsumeTopology<TMessage>
    where TMessage : class
{
}
