namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>
/// Defines the contract for in memory message consume topology.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IInMemoryMessageConsumeTopology<TMessage> :
    IMessageConsumeTopology<TMessage>
    where TMessage : class
{
}
