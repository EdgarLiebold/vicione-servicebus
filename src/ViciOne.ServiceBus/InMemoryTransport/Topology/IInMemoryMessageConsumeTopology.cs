namespace ViciOne.ServiceBus.InMemoryTransport.Topology;

/// <summary>Represents in-memory consume topology for one message contract.</summary>
/// <typeparam name="TMessage">The message contract.</typeparam>
internal interface IInMemoryMessageConsumeTopology<TMessage> :
    IMessageConsumeTopology<TMessage>
    where TMessage : class
{
}
