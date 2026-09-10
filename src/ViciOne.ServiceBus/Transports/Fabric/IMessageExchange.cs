using ViciOne.ServiceBus.Providers.Transports;

namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>Routes in-memory messages to connected destinations.</summary>
/// <typeparam name="TMessage">The message type.</typeparam>
internal interface IMessageExchange<TMessage> :
    IMessageSink<TMessage>,
    IMessageSource<TMessage>
    where TMessage : class
{
    /// <summary>Gets the exchange name.</summary>
    string Name { get; }

    /// <summary>Gets the routing behavior of the exchange.</summary>
    InMemoryExchangeType ExchangeType { get; }
}
