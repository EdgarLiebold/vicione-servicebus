using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for message fabric topology builder.
/// </summary>
public interface IMessageFabricTopologyBuilder
{
    /// <summary>
    /// Performs the exchange bind operation.
    /// </summary>
    /// <param name="source">The source value.</param>
    /// <param name="destination">The destination value.</param>
    /// <param name="routingKey">The routing key value.</param>
    void ExchangeBind(string source, string destination, string? routingKey);

    /// <summary>
    /// Performs the queue bind operation.
    /// </summary>
    /// <param name="source">The source value.</param>
    /// <param name="destination">The destination value.</param>
    void QueueBind(string source, string destination);

    /// <summary>
    /// Performs the exchange declare operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="exchangeType">The exchange type value.</param>
    void ExchangeDeclare(string name, ExchangeType exchangeType);

    /// <summary>
    /// Performs the queue declare operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    void QueueDeclare(string name);
}
