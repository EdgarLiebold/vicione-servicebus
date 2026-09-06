using ViciOne.ServiceBus.RabbitMq.Topology;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Selects the RabbitMQ exchange type for one message contract.</summary>
/// <typeparam name="TMessage">The message contract.</typeparam>
public class MessageExchangeTypeSelector<TMessage> :
    IMessageExchangeTypeSelector<TMessage>
    where TMessage : class
{
    readonly IExchangeTypeSelector _exchangeTypeSelector;

    /// <summary>Creates a typed view over an exchange-type selector.</summary>
    /// <param name="exchangeTypeSelector">The selector that resolves exchange-name conventions.</param>
    public MessageExchangeTypeSelector(IExchangeTypeSelector exchangeTypeSelector)
    {
        _exchangeTypeSelector = exchangeTypeSelector;
    }

    /// <summary>Gets the fallback exchange type from the untyped selector.</summary>
    public string DefaultExchangeType => _exchangeTypeSelector.DefaultExchangeType;

    /// <summary>Gets the exchange type selected for a named message exchange.</summary>
    /// <param name="exchangeName">The exchange name.</param>
    /// <returns>The RabbitMQ exchange type selected for <typeparamref name="TMessage"/>.</returns>
    public string GetExchangeType(string exchangeName)
    {
        return _exchangeTypeSelector.GetExchangeType<TMessage>(exchangeName);
    }
}
