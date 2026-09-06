using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Configures in memory message publish topology.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IInMemoryMessagePublishTopologyConfigurator<TMessage> :
    IMessagePublishTopologyConfigurator<TMessage>,
    IInMemoryMessagePublishTopology<TMessage>,
    IInMemoryMessagePublishTopologyConfigurator
    where TMessage : class
{
    /// <summary>Gets or sets the exchange type.</summary>
    new ExchangeType ExchangeType { set; }
}


/// <summary>Configures in memory message publish topology.</summary>
public interface IInMemoryMessagePublishTopologyConfigurator :
    IMessagePublishTopologyConfigurator
{
}
