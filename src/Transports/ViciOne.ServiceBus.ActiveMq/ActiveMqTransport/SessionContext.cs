using System.Threading;
using System.Threading.Tasks;
using Apache.NMS;
using ViciOne.ServiceBus.ActiveMq.Topology;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>
/// Defines the contract for session context.
/// </summary>
public interface SessionContext :
    PipeContext
{
    /// <summary>
    /// Gets the session value.
    /// </summary>
    ISession Session { get; }

    /// <summary>
    /// Gets the connection context value.
    /// </summary>
    ConnectionContext ConnectionContext { get; }

    /// <summary>
    /// Gets topic.
    /// </summary>
    /// <param name="topic">The topic value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<ITopic> GetTopicAsync(Topic topic, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets queue.
    /// </summary>
    /// <param name="queue">The queue value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<IQueue> GetQueueAsync(Queue queue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Makes the broker hold this topic, so that a deployed publish topology exists on the broker
    /// and not only in the client.
    /// <para>
    /// Resolving a topic name only creates an NMS destination object and does not materialize the
    /// topic on the broker. Opening and closing a producer materializes the destination without
    /// creating the subscription and delivery semantics that a consumer would introduce.
    /// </para>
    /// <para>
    /// The caller asks for the outcome. How the outcome is reached is a property of the session,
    /// and a filter that orchestrated the NMS steps itself would own an implementation detail it
    /// cannot see the consequences of.
    /// </para>
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <param name="topic">The topic used by the operation.</param>
    Task EnsureTopicExistsAsync(Topic topic, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets destination.
    /// </summary>
    /// <param name="destinationName">The destination name value.</param>
    /// <param name="destinationType">The destination type value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<IDestination> GetDestinationAsync(string destinationName, DestinationType destinationType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates message consumer.
    /// </summary>
    /// <param name="destination">The destination value.</param>
    /// <param name="selector">The selector value.</param>
    /// <param name="noLocal">The no local value.</param>
    /// <param name="consumerName">The consumer name value.</param>
    /// <param name="shared">The shared value.</param>
    /// <param name="durable">The durable value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<IMessageConsumer> CreateMessageConsumerAsync(
        IDestination destination,
        string? selector,
        bool noLocal,
        string? consumerName = null,
        bool shared = false,
        bool durable = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="destination">The destination value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task SendAsync(IDestination destination, IMessage message, CancellationToken cancellationToken);

    /// <summary>
    /// Creates bytes message.
    /// </summary>
    /// <param name="content">The content value.</param>
    /// <returns>The result of the operation.</returns>
    IBytesMessage CreateBytesMessage(byte[] content);

    /// <summary>
    /// Creates text message.
    /// </summary>
    /// <param name="content">The content value.</param>
    /// <returns>The result of the operation.</returns>
    ITextMessage CreateTextMessage(string content);

    /// <summary>
    /// Creates message.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    IMessage CreateMessage();

    /// <summary>
    /// Performs the delete topic operation.
    /// </summary>
    /// <param name="topicName">The topic name value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task DeleteTopicAsync(string topicName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs the delete queue operation.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task DeleteQueueAsync(string queueName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets temporary destination.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <returns>The result of the operation.</returns>
    IDestination? GetTemporaryDestination(string name);
}
