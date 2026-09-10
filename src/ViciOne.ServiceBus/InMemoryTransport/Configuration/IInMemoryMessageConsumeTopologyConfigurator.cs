using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.InMemoryTransport.Configuration;

/// <summary>Configures an in-memory exchange binding for one consumed message contract.</summary>
/// <typeparam name="TMessage">The consumed message contract.</typeparam>
internal interface IInMemoryMessageConsumeTopologyConfigurator<TMessage> :
    IMessageConsumeTopologyConfigurator<TMessage>,
    IInMemoryMessageConsumeTopology<TMessage>
    where TMessage : class
{
    /// <summary>Adds the publish-exchange binding for this message contract.</summary>
    /// <param name="exchangeType">An exchange routing override, or <see langword="null" /> to use publish topology.</param>
    /// <param name="routingKey">The optional direct or topic routing key.</param>
    void Bind(ExchangeType? exchangeType = default, string? routingKey = null);
}


/// <summary>Applies consume topology for a runtime message contract.</summary>
internal interface IInMemoryMessageConsumeTopologyConfigurator :
    IMessageConsumeTopologyConfigurator
{
    /// <summary>Applies the message exchange bindings to a consume topology builder.</summary>
    /// <param name="builder">The consume topology builder to update.</param>
    void Apply(IMessageFabricConsumeTopologyBuilder builder);
}
