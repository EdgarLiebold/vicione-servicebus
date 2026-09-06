using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Builds message fabric topology components.</summary>
public interface IMessageFabricTopologyBuilder
{
    /// <summary>Binds the configured exchange.</summary>
    /// <param name="source">The source value.</param>
    /// <param name="destination">The destination.</param>
    /// <param name="routingKey">The routing key.</param>
    void ExchangeBind(string source, string destination, string? routingKey);

    /// <summary>Binds the configured queue.</summary>
    /// <param name="source">The source value.</param>
    /// <param name="destination">The destination.</param>
    void QueueBind(string source, string destination);

    /// <summary>Declares the configured exchange.</summary>
    /// <param name="name">The name.</param>
    /// <param name="exchangeType">The runtime exchange type used by the operation.</param>
    void ExchangeDeclare(string name, ExchangeType exchangeType);

    /// <summary>Declares the configured queue.</summary>
    /// <param name="name">The name.</param>
    void QueueDeclare(string name);
}
