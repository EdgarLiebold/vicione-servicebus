namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Configures how an in-memory exchange publishes one message contract.</summary>
/// <typeparam name="TMessage">The message contract.</typeparam>
public interface IInMemoryMessagePublishTopologyConfigurator<TMessage> :
    IMessagePublishTopologyConfigurator<TMessage>,
    IInMemoryMessagePublishTopologyConfigurator
    where TMessage : class
{
    /// <summary>Sets the exchange routing behavior.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The assigned value is not a defined <see cref="InMemoryExchangeType" />.</exception>
    InMemoryExchangeType ExchangeType { set; }
}


/// <summary>Configures in-memory publish topology for a runtime message contract.</summary>
public interface IInMemoryMessagePublishTopologyConfigurator :
    IMessagePublishTopologyConfigurator
{
}
