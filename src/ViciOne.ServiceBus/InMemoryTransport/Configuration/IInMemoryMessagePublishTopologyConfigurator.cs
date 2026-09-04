using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>
/// Defines the contract for in memory message publish topology configurator.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IInMemoryMessagePublishTopologyConfigurator<TMessage> :
    IMessagePublishTopologyConfigurator<TMessage>,
    IInMemoryMessagePublishTopology<TMessage>,
    IInMemoryMessagePublishTopologyConfigurator
    where TMessage : class
{
    /// <summary>
    /// Gets or sets the exchange type value.
    /// </summary>
    new ExchangeType ExchangeType { set; }
}


/// <summary>
/// Defines the contract for in memory message publish topology configurator.
/// </summary>
public interface IInMemoryMessagePublishTopologyConfigurator :
    IMessagePublishTopologyConfigurator
{
}
