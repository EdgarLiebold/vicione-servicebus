using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports.Fabric;

#nullable enable
namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>
/// Defines the contract for in memory message consume topology configurator.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IInMemoryMessageConsumeTopologyConfigurator<TMessage> :
    IMessageConsumeTopologyConfigurator<TMessage>,
    IInMemoryMessageConsumeTopology<TMessage>
    where TMessage : class
{
    /// <summary>
    /// Adds the exchange bindings for this message type
    /// </summary>
    void Bind(ExchangeType? exchangeType = default, string? routingKey = null);
}


/// <summary>
/// Defines the contract for in memory message consume topology configurator.
/// </summary>
public interface IInMemoryMessageConsumeTopologyConfigurator :
    IMessageConsumeTopologyConfigurator
{
    /// <summary>
    /// Apply the message topology to the builder
    /// </summary>
    /// <param name="builder"></param>
    void Apply(IMessageFabricConsumeTopologyBuilder builder);
}
