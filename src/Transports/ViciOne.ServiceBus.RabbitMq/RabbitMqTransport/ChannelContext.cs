using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// With a connection, and a channel from RabbitMQ, this context is passed forward to allow
/// the channel to be configured and connected
/// </summary>
public interface ChannelContext :
    PipeContext
{
    /// <summary>
    /// The channel
    /// </summary>
    IChannel Channel { get; }

    /// <summary>
    /// The connection context on which the channel was created
    /// </summary>
    ConnectionContext ConnectionContext { get; }

    /// <summary>
    /// Publishes a message to RabbitMQ asynchronously.
    /// </summary>
    /// <param name="exchange">The destination exchange</param>
    /// <param name="routingKey">The exchange routing key</param>
    /// <param name="mandatory">true if the message must be delivered</param>
    /// <param name="basicProperties">The message properties</param>
    /// <param name="body">The message body</param>
    /// <param name="awaitAck"><see langword="true"/> to await RabbitMQ publisher confirmation before completing; otherwise, the publish remains internally observed.</param>
    /// <param name="cancellationToken">The token used to cancel the publish operation.</param>
    /// <returns>
    /// A task that completes after publisher confirmation when <paramref name="awaitAck"/> is <see langword="true"/>;
    /// otherwise, it completes after the RabbitMQ client accepts the publish operation.
    /// </returns>
    Task BasicPublishAsync(string exchange, string routingKey, bool mandatory, BasicProperties basicProperties, byte[] body, bool awaitAck, CancellationToken cancellationToken);

    /// <summary>
    /// Performs the exchange bind operation.
    /// </summary>
    /// <param name="destination">The destination value.</param>
    /// <param name="source">The source value.</param>
    /// <param name="routingKey">The routing key value.</param>
    /// <param name="arguments">The arguments value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task ExchangeBindAsync(string destination, string source, string routingKey, IDictionary<string, object?> arguments, CancellationToken cancellationToken);
    /// <summary>
    /// Performs the exchange declare operation.
    /// </summary>
    /// <param name="exchange">The exchange value.</param>
    /// <param name="type">The type value.</param>
    /// <param name="durable">The durable value.</param>
    /// <param name="autoDelete">The auto delete value.</param>
    /// <param name="arguments">The arguments value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task ExchangeDeclareAsync(string exchange, string type, bool durable, bool autoDelete, IDictionary<string, object?> arguments, CancellationToken cancellationToken);
    /// <summary>
    /// Performs the exchange declare passive operation.
    /// </summary>
    /// <param name="exchange">The exchange value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task ExchangeDeclarePassiveAsync(string exchange, CancellationToken cancellationToken);

    /// <summary>
    /// Performs the queue bind operation.
    /// </summary>
    /// <param name="queue">The queue value.</param>
    /// <param name="exchange">The exchange value.</param>
    /// <param name="routingKey">The routing key value.</param>
    /// <param name="arguments">The arguments value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task QueueBindAsync(string queue, string exchange, string routingKey, IDictionary<string, object?> arguments, CancellationToken cancellationToken);
    /// <summary>
    /// Performs the queue declare operation.
    /// </summary>
    /// <param name="queue">The queue value.</param>
    /// <param name="durable">The durable value.</param>
    /// <param name="exclusive">The exclusive value.</param>
    /// <param name="autoDelete">The auto delete value.</param>
    /// <param name="arguments">The arguments value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<QueueDeclareOk> QueueDeclareAsync(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object?> arguments, CancellationToken cancellationToken);
    /// <summary>
    /// Performs the queue declare passive operation.
    /// </summary>
    /// <param name="queue">The queue value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<QueueDeclareOk> QueueDeclarePassiveAsync(string queue, CancellationToken cancellationToken);

    /// <summary>
    /// Performs the queue purge operation.
    /// </summary>
    /// <param name="queue">The queue value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<uint> QueuePurgeAsync(string queue, CancellationToken cancellationToken);

    /// <summary>
    /// Performs the basic qos operation.
    /// </summary>
    /// <param name="prefetchSize">The prefetch size value.</param>
    /// <param name="prefetchCount">The prefetch count value.</param>
    /// <param name="global">The global value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task BasicQosAsync(uint prefetchSize, ushort prefetchCount, bool global, CancellationToken cancellationToken);

    /// <summary>
    /// Performs the basic ack operation.
    /// </summary>
    /// <param name="deliveryTag">The delivery tag value.</param>
    /// <param name="multiple">The multiple value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    ValueTask BasicAckAsync(ulong deliveryTag, bool multiple, CancellationToken cancellationToken);

    /// <summary>
    /// Performs the basic nack operation.
    /// </summary>
    /// <param name="deliveryTag">The delivery tag value.</param>
    /// <param name="multiple">The multiple value.</param>
    /// <param name="requeue">The requeue value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task BasicNackAsync(ulong deliveryTag, bool multiple, bool requeue, CancellationToken cancellationToken);

    /// <summary>
    /// Performs the basic consume operation.
    /// </summary>
    /// <param name="queue">The queue value.</param>
    /// <param name="noAck">The no ack value.</param>
    /// <param name="exclusive">The exclusive value.</param>
    /// <param name="arguments">The arguments value.</param>
    /// <param name="consumer">The consumer value.</param>
    /// <param name="consumerTag">The consumer tag value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<string> BasicConsumeAsync(string queue, bool noAck, bool exclusive, IDictionary<string, object?> arguments, IAsyncBasicConsumer consumer,
        string consumerTag, CancellationToken cancellationToken);

    /// <summary>
    /// Performs the basic cancel operation.
    /// </summary>
    /// <param name="consumerTag">The consumer tag value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task BasicCancelAsync(string consumerTag, CancellationToken cancellationToken);

    /// <summary>
    /// Performs the notify faulted operation.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="inputAddress">The input address value.</param>
    void NotifyFaulted(Exception exception, Uri inputAddress);
}
