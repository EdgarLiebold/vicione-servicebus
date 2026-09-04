namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>
/// Defines the contract for sql message consume topology.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface ISqlMessageConsumeTopology<TMessage> :
    IMessageConsumeTopology<TMessage>
    where TMessage : class
{
}
