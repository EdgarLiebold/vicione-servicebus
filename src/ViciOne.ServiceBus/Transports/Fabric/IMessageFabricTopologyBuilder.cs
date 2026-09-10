using ViciOne.ServiceBus.Providers.Transports;

namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>Applies exchange and queue declarations to an in-memory message fabric.</summary>
internal interface IMessageFabricTopologyBuilder
{
    /// <summary>Connects a source exchange to a destination exchange.</summary>
    /// <param name="source">The source exchange name.</param>
    /// <param name="destination">The destination exchange name.</param>
    /// <param name="routingKey">The routing key or topic pattern applied by the source.</param>
    void ExchangeBind(string source, string destination, string? routingKey);

    /// <summary>Connects a source exchange to a destination queue.</summary>
    /// <param name="source">The source exchange name.</param>
    /// <param name="destination">The destination queue name.</param>
    void QueueBind(string source, string destination);

    /// <summary>Declares an exchange.</summary>
    /// <param name="name">The exchange name.</param>
    /// <param name="exchangeType">The exchange routing behavior.</param>
    void ExchangeDeclare(string name, InMemoryExchangeType exchangeType);

    /// <summary>Declares a queue.</summary>
    /// <param name="name">The queue name.</param>
    void QueueDeclare(string name);
}
