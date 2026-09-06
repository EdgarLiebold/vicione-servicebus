using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Configures in memory message consume topology.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IInMemoryMessageConsumeTopologyConfigurator<TMessage> :
    IMessageConsumeTopologyConfigurator<TMessage>,
    IInMemoryMessageConsumeTopology<TMessage>
    where TMessage : class
{
    /// <summary>Adds the exchange bindings for this message type.</summary>
    /// <param name="exchangeType">The runtime exchange type used by the operation.</param>
    /// <param name="routingKey">The routing key.</param>
    void Bind(ExchangeType? exchangeType = default, string? routingKey = null);
}


/// <summary>Configures in memory message consume topology.</summary>
public interface IInMemoryMessageConsumeTopologyConfigurator :
    IMessageConsumeTopologyConfigurator
{
    /// <summary>Apply the message topology to the builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    void Apply(IMessageFabricConsumeTopologyBuilder builder);
}
