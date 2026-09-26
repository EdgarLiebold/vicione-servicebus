using System.Threading;
using System.Threading.Tasks;
using Apache.NMS;
using ViciOne.ServiceBus.ActiveMq.Topology;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Exposes serialized Apache NMS session and destination operations.</summary>
public interface SessionContext :
    PipeContext
{
    /// <summary>Gets the underlying Apache NMS session.</summary>
    ISession Session { get; }

    /// <summary>Gets the owning ActiveMQ connection context.</summary>
    ConnectionContext ConnectionContext { get; }

    /// <summary>Resolves a native topic, using a registered temporary topic when appropriate.</summary>
    /// <param name="topic">The configured broker topic.</param>
    /// <param name="cancellationToken">The token used to cancel resolution.</param>
    /// <returns>A task that produces the native topic destination.</returns>
    Task<ITopic> GetTopicAsync(Topic topic, CancellationToken cancellationToken = default);

    /// <summary>Resolves a native queue, using a registered temporary queue when appropriate.</summary>
    /// <param name="queue">The configured broker queue.</param>
    /// <param name="cancellationToken">The token used to cancel resolution.</param>
    /// <returns>A task that produces the native queue destination.</returns>
    Task<IQueue> GetQueueAsync(Queue queue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures a publish topic exists on the broker rather than only as a client-side destination object.
    /// <para>
    /// Resolving a topic name creates only an Apache NMS destination object. Opening and closing a
    /// producer materializes the broker destination without introducing consumer subscription semantics.
    /// </para>
    /// <para>
    /// The session owns the provider-specific materialization sequence.
    /// </para>
    /// </summary>
    /// <param name="topic">The broker topic to materialize.</param>
    /// <param name="cancellationToken">The token used to cancel materialization.</param>
    /// <returns>A task that completes when the broker has confirmed topic availability.</returns>
    Task EnsureTopicExistsAsync(Topic topic, CancellationToken cancellationToken = default);

    /// <summary>Resolves a registered temporary destination or creates a native destination.</summary>
    /// <param name="destinationName">The destination name.</param>
    /// <param name="destinationType">The Apache NMS destination type.</param>
    /// <param name="cancellationToken">The token used to cancel resolution.</param>
    /// <returns>A task that produces the native destination.</returns>
    Task<IDestination> GetDestinationAsync(string destinationName, DestinationType destinationType, CancellationToken cancellationToken = default);

    /// <summary>Creates a native queue consumer or an appropriately shared and durable topic consumer.</summary>
    /// <param name="destination">The native destination to consume.</param>
    /// <param name="selector">An optional Apache NMS message selector.</param>
    /// <param name="noLocal">Whether messages produced by this connection must be excluded.</param>
    /// <param name="consumerName">The subscription name for a topic consumer.</param>
    /// <param name="shared">Whether a named Artemis AMQP topic subscription is shared.</param>
    /// <param name="durable">Whether a named topic subscription is durable.</param>
    /// <param name="cancellationToken">The token used to cancel consumer creation.</param>
    /// <returns>A task that produces the native message consumer.</returns>
    Task<IMessageConsumer> CreateMessageConsumerAsync(
        IDestination destination,
        string? selector,
        bool noLocal,
        string? consumerName = null,
        bool shared = false,
        bool durable = true, CancellationToken cancellationToken = default);

    /// <summary>Sends a native message through the cached producer for a destination.</summary>
    /// <param name="destination">The native destination.</param>
    /// <param name="message">The Apache NMS message to send.</param>
    /// <param name="cancellationToken">The token used to cancel producer acquisition and sending.</param>
    /// <returns>A task that completes when the native send completes.</returns>
    Task SendAsync(IDestination destination, IMessage message, CancellationToken cancellationToken);

    /// <summary>Creates a native byte message in this session.</summary>
    /// <param name="content">The message body bytes.</param>
    /// <returns>The created Apache NMS byte message.</returns>
    IBytesMessage CreateBytesMessage(byte[] content);

    /// <summary>Creates a native text message in this session.</summary>
    /// <param name="content">The message body text.</param>
    /// <returns>The created Apache NMS text message.</returns>
    ITextMessage CreateTextMessage(string content);

    /// <summary>Creates a native message without a typed body in this session.</summary>
    /// <returns>The created Apache NMS message.</returns>
    IMessage CreateMessage();

    /// <summary>Deletes a registered temporary topic or a named broker topic.</summary>
    /// <param name="topicName">The topic name.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when deletion has finished.</returns>
    Task DeleteTopicAsync(string topicName, CancellationToken cancellationToken = default);

    /// <summary>Deletes a registered temporary queue or a named broker queue.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when deletion has finished.</returns>
    Task DeleteQueueAsync(string queueName, CancellationToken cancellationToken = default);

    /// <summary>Gets a registered temporary destination by name and destination type.</summary>
    /// <param name="name">The destination name.</param>
    /// <param name="destinationType">The queue or topic destination type.</param>
    /// <returns>The temporary destination, or <see langword="null" /> when it is not registered.</returns>
    IDestination? GetTemporaryDestination(string name, DestinationType destinationType);
}
