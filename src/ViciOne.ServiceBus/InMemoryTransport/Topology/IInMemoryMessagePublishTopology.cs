using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Providers.Transports;
using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.InMemoryTransport.Topology;

/// <summary>Represents in-memory publish topology for one message contract.</summary>
/// <typeparam name="TMessage">The message contract.</typeparam>
internal interface IInMemoryMessagePublishTopology<TMessage> :
    IMessagePublishTopology<TMessage>,
    IInMemoryMessagePublishTopology
    where TMessage : class
{
    /// <summary>Gets the exchange routing behavior.</summary>
    InMemoryExchangeType ExchangeType { get; }
}


/// <summary>Applies in-memory publish topology to a message-fabric builder.</summary>
internal interface IInMemoryMessagePublishTopology
{
    /// <summary>Declares and binds the configured exchanges.</summary>
    /// <param name="builder">The publish topology builder to update.</param>
    void Apply(IMessageFabricPublishTopologyBuilder builder);
}
