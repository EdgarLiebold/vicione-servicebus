using RabbitMQ.Client;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Exposes the AMQP delivery metadata supplied to the consumer bound to the inbound channel.
/// </summary>
public interface RabbitMqBasicConsumeContext :
    RoutingKeyConsumeContext
{
    /// <summary>The exchange from which RabbitMQ routed the message.</summary>
    string Exchange { get; }

    /// <summary>The consumer tag of the receiving consumer.</summary>
    string ConsumerTag { get; }

    /// <summary>The delivery tag of the message to the consumer.</summary>
    ulong DeliveryTag { get; }

    /// <summary>The basic properties of the message.</summary>
    IReadOnlyBasicProperties Properties { get; }
}
