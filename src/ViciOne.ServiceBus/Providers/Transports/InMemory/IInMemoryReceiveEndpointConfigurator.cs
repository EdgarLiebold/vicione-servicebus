using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Configures an in-memory receive endpoint and its exchange bindings.</summary>
public interface IInMemoryReceiveEndpointConfigurator :
    IReceiveEndpointConfigurator
{
    /// <summary>Binds a named exchange to the receive endpoint queue.</summary>
    /// <param name="exchangeName">The non-empty exchange name.</param>
    /// <param name="exchangeType">The exchange routing behavior.</param>
    /// <param name="routingKey">The optional key used by direct or topic routing.</param>
    void Bind(string exchangeName, ExchangeType exchangeType = ExchangeType.FanOut, string? routingKey = default);

    /// <summary>Binds the exchange for a message contract to the receive endpoint queue.</summary>
    /// <typeparam name="TMessage">The message contract.</typeparam>
    /// <param name="exchangeType">The exchange routing behavior.</param>
    /// <param name="routingKey">The optional key used by direct or topic routing.</param>
    void Bind<TMessage>(ExchangeType exchangeType = ExchangeType.FanOut, string? routingKey = default)
        where TMessage : class;
}
