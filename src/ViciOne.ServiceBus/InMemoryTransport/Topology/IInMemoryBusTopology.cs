namespace ViciOne.ServiceBus.InMemoryTransport.Topology;

/// <summary>Exposes message-specific publish topology for an in-memory bus.</summary>
internal interface IInMemoryBusTopology :
    IBusTopology
{
    /// <summary>Gets publish topology for a message contract.</summary>
    /// <typeparam name="TMessage">The message contract.</typeparam>
    /// <returns>The message-specific publish topology.</returns>
    new IInMemoryMessagePublishTopology<TMessage> Publish<TMessage>()
        where TMessage : class;
}
