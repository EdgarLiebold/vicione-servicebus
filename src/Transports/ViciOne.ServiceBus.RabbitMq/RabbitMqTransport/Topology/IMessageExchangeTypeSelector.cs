namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>Selects the RabbitMQ exchange type for one published message contract.</summary>
/// <typeparam name="TMessage">The message type.</typeparam>
public interface IMessageExchangeTypeSelector<in TMessage>
    where TMessage : class
{
    /// <summary>Gets the exchange type used when no exchange-name-specific selection applies.</summary>
    string DefaultExchangeType { get; }

    /// <summary>Returns the exchange type for the message type.</summary>
    /// <param name="exchangeName">The exchange name.</param>
    /// <returns>The RabbitMQ exchange type to declare.</returns>
    string GetExchangeType(string exchangeName);
}
