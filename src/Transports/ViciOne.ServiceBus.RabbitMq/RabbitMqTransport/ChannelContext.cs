using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Exposes one RabbitMQ channel, its owning connection, and the AMQP operations used by transport pipelines.
/// </summary>
public interface ChannelContext :
    PipeContext
{
    /// <summary>Gets the active RabbitMQ channel.</summary>
    IChannel Channel { get; }

    /// <summary>The connection context on which the channel was created.</summary>
    ConnectionContext ConnectionContext { get; }

    /// <summary>Publishes a message to RabbitMQ asynchronously.</summary>
    /// <param name="exchange">The destination exchange.</param>
    /// <param name="routingKey">The exchange routing key.</param>
    /// <param name="mandatory">Whether RabbitMQ must return the message when no queue can accept it.</param>
    /// <param name="basicProperties">The message properties.</param>
    /// <param name="body">The message body.</param>
    /// <param name="awaitAck"><see langword="true"/> to await the RabbitMQ client publish task, including publisher confirmation when enabled; otherwise, the publish remains internally observed.</param>
    /// <param name="cancellationToken">The token used to cancel the publish operation.</param>
    /// <returns>
    /// A task that follows the RabbitMQ client publish operation when <paramref name="awaitAck"/> is <see langword="true"/>;
    /// otherwise, it completes after initiation while the transport observes the client operation internally.
    /// </returns>
    Task BasicPublishAsync(string exchange, string routingKey, bool mandatory, BasicProperties basicProperties, byte[] body, bool awaitAck, CancellationToken cancellationToken);

    /// <summary>Creates an exchange-to-exchange binding.</summary>
    /// <param name="destination">The destination exchange.</param>
    /// <param name="source">The source exchange.</param>
    /// <param name="routingKey">The binding routing key.</param>
    /// <param name="arguments">The RabbitMQ binding arguments.</param>
    /// <param name="cancellationToken">Cancellation for the broker command.</param>
    /// <returns>A task that completes when RabbitMQ accepts the binding.</returns>
    Task ExchangeBindAsync(string destination, string source, string routingKey, IDictionary<string, object?> arguments, CancellationToken cancellationToken);
    /// <summary>Declares an exchange with the supplied RabbitMQ topology settings.</summary>
    /// <param name="exchange">The exchange name.</param>
    /// <param name="type">The RabbitMQ exchange type.</param>
    /// <param name="durable">Whether the exchange survives broker restarts.</param>
    /// <param name="autoDelete">Whether RabbitMQ deletes the exchange when unused.</param>
    /// <param name="arguments">The exchange declaration arguments.</param>
    /// <param name="cancellationToken">Cancellation for the broker command.</param>
    /// <returns>A task that completes when RabbitMQ accepts the declaration.</returns>
    Task ExchangeDeclareAsync(string exchange, string type, bool durable, bool autoDelete, IDictionary<string, object?> arguments, CancellationToken cancellationToken);
    /// <summary>Passively verifies that an exchange exists and matches the broker declaration.</summary>
    /// <param name="exchange">The exchange name.</param>
    /// <param name="cancellationToken">Cancellation for the broker command.</param>
    /// <returns>A task that completes when RabbitMQ confirms the exchange exists.</returns>
    Task ExchangeDeclarePassiveAsync(string exchange, CancellationToken cancellationToken);

    /// <summary>Creates an exchange-to-queue binding.</summary>
    /// <param name="queue">The destination queue.</param>
    /// <param name="exchange">The source exchange.</param>
    /// <param name="routingKey">The binding routing key.</param>
    /// <param name="arguments">The RabbitMQ binding arguments.</param>
    /// <param name="cancellationToken">Cancellation for the broker command.</param>
    /// <returns>A task that completes when RabbitMQ accepts the binding.</returns>
    Task QueueBindAsync(string queue, string exchange, string routingKey, IDictionary<string, object?> arguments, CancellationToken cancellationToken);
    /// <summary>Declares a queue with the supplied RabbitMQ topology settings.</summary>
    /// <param name="queue">The queue name, or an empty string for a broker-generated name.</param>
    /// <param name="durable">Whether the queue survives broker restarts.</param>
    /// <param name="exclusive">Whether the queue belongs exclusively to this connection.</param>
    /// <param name="autoDelete">Whether RabbitMQ deletes the queue after its last consumer is gone.</param>
    /// <param name="arguments">The queue declaration arguments.</param>
    /// <param name="cancellationToken">Cancellation for the broker command.</param>
    /// <returns>The broker's queue declaration result.</returns>
    Task<QueueDeclareOk> QueueDeclareAsync(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object?> arguments, CancellationToken cancellationToken);
    /// <summary>Passively verifies that a queue exists and returns its current broker state.</summary>
    /// <param name="queue">The queue name.</param>
    /// <param name="cancellationToken">Cancellation for the broker command.</param>
    /// <returns>The broker's passive queue declaration result.</returns>
    Task<QueueDeclareOk> QueueDeclarePassiveAsync(string queue, CancellationToken cancellationToken);

    /// <summary>Removes every pending message from the queue.</summary>
    /// <param name="queue">The queue name.</param>
    /// <param name="cancellationToken">Cancellation for the broker command.</param>
    /// <returns>The number of messages removed.</returns>
    Task<uint> QueuePurgeAsync(string queue, CancellationToken cancellationToken);

    /// <summary>Applies the configured RabbitMQ quality-of-service limits.</summary>
    /// <param name="prefetchSize">The AMQP prefetch-size limit.</param>
    /// <param name="prefetchCount">The maximum number of unacknowledged deliveries.</param>
    /// <param name="global">Whether the limit applies to every consumer on the channel.</param>
    /// <param name="cancellationToken">Cancellation for the broker command.</param>
    /// <returns>A task that completes when RabbitMQ applies the limit.</returns>
    Task BasicQosAsync(uint prefetchSize, ushort prefetchCount, bool global, CancellationToken cancellationToken);

    /// <summary>Acknowledges the selected RabbitMQ delivery.</summary>
    /// <param name="deliveryTag">The channel-scoped delivery tag.</param>
    /// <param name="multiple">Whether to acknowledge this tag and every preceding unacknowledged delivery.</param>
    /// <param name="cancellationToken">Cancellation for writing the acknowledgement.</param>
    /// <returns>A task-like value that completes after the acknowledgement is written.</returns>
    ValueTask BasicAckAsync(ulong deliveryTag, bool multiple, CancellationToken cancellationToken);

    /// <summary>Rejects the selected RabbitMQ delivery.</summary>
    /// <param name="deliveryTag">The channel-scoped delivery tag.</param>
    /// <param name="multiple">Whether to reject this tag and every preceding unacknowledged delivery.</param>
    /// <param name="requeue">Whether RabbitMQ should place rejected deliveries back on their queues.</param>
    /// <param name="cancellationToken">Cancellation for writing the rejection.</param>
    /// <returns>A task that completes after the negative acknowledgement is written.</returns>
    Task BasicNackAsync(ulong deliveryTag, bool multiple, bool requeue, CancellationToken cancellationToken);

    /// <summary>Starts the configured RabbitMQ consumer.</summary>
    /// <param name="queue">The source queue name.</param>
    /// <param name="noAck">Whether RabbitMQ should consider deliveries acknowledged immediately.</param>
    /// <param name="exclusive">Whether the broker permits only this consumer on the queue.</param>
    /// <param name="arguments">The consumer arguments.</param>
    /// <param name="consumer">The callback receiver for deliveries and lifecycle events.</param>
    /// <param name="consumerTag">The requested consumer tag, or an empty string for a generated tag.</param>
    /// <param name="cancellationToken">Cancellation for starting the consumer.</param>
    /// <returns>The consumer tag assigned by RabbitMQ.</returns>
    Task<string> BasicConsumeAsync(string queue, bool noAck, bool exclusive, IDictionary<string, object?> arguments, IAsyncBasicConsumer consumer,
        string consumerTag, CancellationToken cancellationToken);

    /// <summary>Cancels the active RabbitMQ consumer.</summary>
    /// <param name="consumerTag">The tag of the consumer to cancel.</param>
    /// <param name="cancellationToken">Cancellation for the broker command.</param>
    /// <returns>A task that completes when the cancel command has been sent.</returns>
    Task BasicCancelAsync(string consumerTag, CancellationToken cancellationToken);

    /// <summary>Reports a channel fault to the receive transport.</summary>
    /// <param name="exception">The channel or consumer failure.</param>
    /// <param name="inputAddress">The receive endpoint address affected by the fault.</param>
    void NotifyFaulted(Exception exception, Uri inputAddress);
}
