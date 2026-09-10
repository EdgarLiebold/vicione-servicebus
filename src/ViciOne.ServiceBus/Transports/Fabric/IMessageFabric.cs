using ViciOne.ServiceBus.Providers.Transports;

namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>Defines topology and lifecycle operations for an in-memory message fabric.</summary>
/// <typeparam name="TMessage">The message envelope type carried by the fabric.</typeparam>
internal interface IMessageFabric<TMessage> :
    IAgent,
    IProbeSite
    where TMessage : class
{
    /// <summary>Gets the clock and delay service shared by the fabric's queues.</summary>
    IInMemoryDelayProvider DelayProvider { get; }

    /// <summary>Declares an exchange or verifies that an existing declaration has the requested routing behavior.</summary>
    /// <param name="name">The exchange name.</param>
    /// <param name="exchangeType">The exchange routing behavior.</param>
    void ExchangeDeclare(string name, InMemoryExchangeType exchangeType);

    /// <summary>Connects a source exchange to a destination exchange.</summary>
    /// <param name="source">The source exchange name.</param>
    /// <param name="destination">The destination exchange name.</param>
    /// <param name="routingKey">The routing key or topic pattern applied by the source.</param>
    void ExchangeBind(string source, string destination, string? routingKey);

    /// <summary>Declares a queue.</summary>
    /// <param name="name">The queue name.</param>
    void QueueDeclare(string name);

    /// <summary>Connects a source exchange to a destination queue.</summary>
    /// <param name="source">The source exchange name.</param>
    /// <param name="destination">The destination queue name.</param>
    void QueueBind(string source, string destination);

    /// <summary>Gets or declares an exchange with the requested routing behavior.</summary>
    /// <param name="name">The exchange name.</param>
    /// <param name="exchangeType">The exchange routing behavior.</param>
    /// <returns>The requested exchange.</returns>
    IMessageExchange<TMessage> GetExchange(
        string name,
        InMemoryExchangeType exchangeType = InMemoryExchangeType.FanOut);

    /// <summary>Gets or declares a queue.</summary>
    /// <param name="name">The queue name.</param>
    /// <returns>The requested queue.</returns>
    IMessageQueue<TMessage> GetQueue(string name);
}
