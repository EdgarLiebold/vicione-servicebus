namespace ViciOne.ServiceBus.InMemoryTransport.Topology;

/// <summary>Exposes message-specific in-memory publish topology.</summary>
internal interface IInMemoryPublishTopology :
    IPublishTopology
{
    /// <summary>Gets publish topology for a message contract.</summary>
    /// <typeparam name="TMessage">The message contract.</typeparam>
    /// <returns>The message-specific publish topology.</returns>
    new IInMemoryMessagePublishTopology<TMessage> GetMessageTopology<TMessage>()
        where TMessage : class;
}
