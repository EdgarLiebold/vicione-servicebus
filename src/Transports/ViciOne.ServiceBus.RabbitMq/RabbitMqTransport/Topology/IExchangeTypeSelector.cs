namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>Selects the RabbitMQ exchange type for a message contract and exchange name during topology construction.</summary>
public interface IExchangeTypeSelector
{
    /// <summary>Gets the exchange type used when no message-specific selection applies.</summary>
    string DefaultExchangeType { get; }

    /// <summary>Selects the exchange type for a message contract.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="exchangeName">The exchange name.</param>
    /// <returns>The RabbitMQ exchange type to declare.</returns>
    string GetExchangeType<T>(string exchangeName)
        where T : class;
}
