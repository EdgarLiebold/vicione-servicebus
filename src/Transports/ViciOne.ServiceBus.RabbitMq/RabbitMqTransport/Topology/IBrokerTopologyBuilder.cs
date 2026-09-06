using System.Collections.Generic;

namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>Collects RabbitMQ exchange, queue, and binding declarations through opaque entity handles.</summary>
public interface IBrokerTopologyBuilder
{
    /// <summary>Adds or reuses an exchange declaration.</summary>
    /// <param name="name">The exchange name.</param>
    /// <param name="type">The exchange type.</param>
    /// <param name="durable">A durable exchange survives a broker restart.</param>
    /// <param name="autoDelete">Whether RabbitMQ deletes the exchange when it is no longer used.</param>
    /// <param name="arguments">The broker-specific exchange declaration arguments.</param>
    /// <returns>A handle used to reference the de-duplicated exchange in subsequent calls.</returns>
    ExchangeHandle ExchangeDeclare(string name, string type, bool durable, bool autoDelete, IDictionary<string, object?> arguments);

    /// <summary>Adds or reuses an exchange-to-exchange binding.</summary>
    /// <param name="source">The source exchange.</param>
    /// <param name="destination">The destination exchange.</param>
    /// <param name="routingKey">The binding routing key.</param>
    /// <param name="arguments">The broker-specific binding arguments.</param>
    /// <returns>A handle used to reference the de-duplicated binding in subsequent calls.</returns>
    ExchangeBindingHandle ExchangeBind(ExchangeHandle source, ExchangeHandle destination, string routingKey, IDictionary<string, object?> arguments);

    /// <summary>Adds or reuses a queue declaration.</summary>
    /// <param name="name">The queue name.</param>
    /// <param name="durable">Whether the queue survives broker restarts.</param>
    /// <param name="autoDelete">Whether RabbitMQ deletes the queue after its last consumer is gone.</param>
    /// <param name="exclusive">Whether the queue belongs exclusively to its declaring connection.</param>
    /// <param name="arguments">The broker-specific queue declaration arguments.</param>
    /// <returns>A handle used to reference the de-duplicated queue in subsequent calls.</returns>
    QueueHandle QueueDeclare(string name, bool durable, bool autoDelete, bool exclusive, IDictionary<string, object?> arguments);

    /// <summary>Adds or reuses an exchange-to-queue binding.</summary>
    /// <param name="exchange">The source exchange handle.</param>
    /// <param name="queue">The destination queue handle.</param>
    /// <param name="routingKey">The routing key used by the binding.</param>
    /// <param name="arguments">The broker-specific binding arguments.</param>
    /// <returns>A handle used to reference the de-duplicated binding in subsequent calls.</returns>
    QueueBindingHandle QueueBind(ExchangeHandle exchange, QueueHandle queue, string routingKey, IDictionary<string, object?> arguments);
}
