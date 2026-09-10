using ViciOne.ServiceBus.InMemoryTransport.Configuration;
using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.InMemoryTransport.Configuration;

/// <summary>Configures endpoint-level and message-level bindings for in-memory consume topology.</summary>
internal interface IInMemoryConsumeTopologyConfigurator :
    IConsumeTopologyConfigurator,
    IInMemoryConsumeTopology
{
    /// <summary>Gets mutable consume topology for a message contract.</summary>
    /// <typeparam name="TMessage">The message contract.</typeparam>
    /// <returns>The message-specific consume topology.</returns>
    new IInMemoryMessageConsumeTopologyConfigurator<TMessage> GetMessageTopology<TMessage>()
        where TMessage : class;

    /// <summary>Adds a consume-topology operation.</summary>
    /// <param name="specification">The operation to add.</param>
    void AddSpecification(IInMemoryConsumeTopologySpecification specification);

    /// <summary>Binds a named exchange to the receive endpoint.</summary>
    /// <param name="exchangeName">The non-empty source exchange name.</param>
    /// <param name="exchangeType">The source exchange routing behavior.</param>
    /// <param name="routingKey">The optional direct or topic routing key.</param>
    void Bind(string exchangeName, ExchangeType exchangeType = ExchangeType.FanOut, string? routingKey = default);
}
