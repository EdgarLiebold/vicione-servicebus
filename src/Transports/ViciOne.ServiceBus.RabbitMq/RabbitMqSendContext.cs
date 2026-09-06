using RabbitMQ.Client;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Exposes RabbitMQ routing, delivery, and publisher-confirm settings for a send.</summary>
public interface RabbitMqSendContext :
    SendContext,
    RoutingKeySendContext
{
    /// <summary>Gets or sets whether RabbitMQ must return the message when no queue is bound for its routing key.</summary>
    bool Mandatory { get; set; }

    /// <summary>Gets the destination exchange.</summary>
    string Exchange { get; }

    /// <summary>
    /// Gets or sets whether the caller waits for the RabbitMQ client publish task, including publisher confirmation when enabled.
    /// When <see langword="false"/>, the caller returns after the client publish has been initiated; the transport continues to
    /// observe the publish task and retain the channel lease internally.
    /// </summary>
    bool AwaitAck { get; set; }

    /// <summary>Gets the AMQP basic properties written with the message.</summary>
    BasicProperties BasicProperties { get; }
}


/// <summary>Exposes RabbitMQ-specific state for a typed send context.</summary>
/// <typeparam name="T">The message type being sent.</typeparam>
public interface RabbitMqSendContext<out T> :
    SendContext<T>,
    RabbitMqSendContext
    where T : class
{
}
