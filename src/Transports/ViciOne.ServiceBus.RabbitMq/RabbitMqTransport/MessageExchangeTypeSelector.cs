using ViciOne.ServiceBus.RabbitMq.Topology;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Provides a message exchange type selector implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class MessageExchangeTypeSelector<TMessage> :
    IMessageExchangeTypeSelector<TMessage>
    where TMessage : class
{
    readonly IExchangeTypeSelector _exchangeTypeSelector;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="exchangeTypeSelector">The exchange type selector value.</param>
    public MessageExchangeTypeSelector(IExchangeTypeSelector exchangeTypeSelector)
    {
        _exchangeTypeSelector = exchangeTypeSelector;
    }

    /// <summary>
    /// Gets the default exchange type value.
    /// </summary>
    public string DefaultExchangeType => _exchangeTypeSelector.DefaultExchangeType;

    /// <summary>
    /// Gets exchange type.
    /// </summary>
    /// <param name="exchangeName">The exchange name value.</param>
    /// <returns>The result of the operation.</returns>
    public string GetExchangeType(string exchangeName)
    {
        return _exchangeTypeSelector.GetExchangeType<TMessage>(exchangeName);
    }
}
