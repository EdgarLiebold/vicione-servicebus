using System.Threading;
using System.Threading.Tasks;
using Apache.NMS;
using ViciOne.ServiceBus.ActiveMq.Topology;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>
/// Provides a scope session context implementation.
/// </summary>
public class ScopeSessionContext :
    ScopePipeContext,
    SessionContext
{
    readonly SessionContext _context;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public ScopeSessionContext(SessionContext context, CancellationToken cancellationToken)
        : base(context)
    {
        _context = context;
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// Gets the cancellation token value.
    /// </summary>
    public override CancellationToken CancellationToken { get; }

    /// <summary>
    /// Gets the session value.
    /// </summary>
    public ISession Session => _context.Session;
    /// <summary>
    /// Gets the connection context value.
    /// </summary>
    public ConnectionContext ConnectionContext => _context.ConnectionContext;

    /// <summary>
    /// Gets topic.
    /// </summary>
    /// <param name="topic">The topic value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<ITopic> GetTopicAsync(Topic topic, CancellationToken cancellationToken = default)
    {
        return _context.GetTopicAsync(topic, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs the ensure topic exists operation.
    /// </summary>
    /// <param name="topic">The topic value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task EnsureTopicExistsAsync(Topic topic, CancellationToken cancellationToken = default)
    {
        return _context.EnsureTopicExistsAsync(topic, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Gets queue.
    /// </summary>
    /// <param name="queue">The queue value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<IQueue> GetQueueAsync(Queue queue, CancellationToken cancellationToken = default)
    {
        return _context.GetQueueAsync(queue, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Gets destination.
    /// </summary>
    /// <param name="destinationName">The destination name value.</param>
    /// <param name="destinationType">The destination type value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<IDestination> GetDestinationAsync(string destinationName, DestinationType destinationType, CancellationToken cancellationToken = default)
    {
        return _context.GetDestinationAsync(destinationName, destinationType, cancellationToken: cancellationToken);
    }

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
    public Task<IMessageConsumer> CreateMessageConsumerAsync(IDestination destination, string? selector, bool noLocal, string? consumerName = null,
        bool shared = false, bool durable = true, CancellationToken cancellationToken = default)
    {
        return _context.CreateMessageConsumerAsync(destination, selector, noLocal, consumerName, shared, durable, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="destination">The destination value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync(IDestination destination, IMessage message, CancellationToken cancellationToken)
    {
        return _context.SendAsync(destination, message, cancellationToken);
    }

    /// <summary>
    /// Creates bytes message.
    /// </summary>
    /// <param name="content">The content value.</param>
    /// <returns>The result of the operation.</returns>
    public IBytesMessage CreateBytesMessage(byte[] content)
    {
        return _context.CreateBytesMessage(content);
    }

    /// <summary>
    /// Performs the delete topic operation.
    /// </summary>
    /// <param name="topicName">The topic name value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task DeleteTopicAsync(string topicName, CancellationToken cancellationToken = default)
    {
        return _context.DeleteTopicAsync(topicName, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs the delete queue operation.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task DeleteQueueAsync(string queueName, CancellationToken cancellationToken = default)
    {
        return _context.DeleteQueueAsync(queueName, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Gets temporary destination.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <returns>The result of the operation.</returns>
    public IDestination? GetTemporaryDestination(string name)
    {
        return _context.GetTemporaryDestination(name);
    }

    /// <summary>
    /// Creates text message.
    /// </summary>
    /// <param name="content">The content value.</param>
    /// <returns>The result of the operation.</returns>
    public ITextMessage CreateTextMessage(string content)
    {
        return _context.CreateTextMessage(content);
    }

    /// <summary>
    /// Creates message.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IMessage CreateMessage()
    {
        return _context.CreateMessage();
    }
}
