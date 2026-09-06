using System.Threading;
using System.Threading.Tasks;
using Apache.NMS;
using ViciOne.ServiceBus.ActiveMq.Topology;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Applies an operation-specific cancellation scope to a shared Apache NMS session context.</summary>
public class SharedSessionContext :
    ProxyPipeContext,
    SessionContext
{
    readonly SessionContext _context;

    /// <summary>Creates a scoped proxy over a shared session context.</summary>
    /// <param name="context">The underlying session context.</param>
    /// <param name="cancellationToken">The token associated with this operation scope.</param>
    public SharedSessionContext(SessionContext context, CancellationToken cancellationToken)
        : base(context)
    {
        _context = context;
        CancellationToken = cancellationToken;
    }

    /// <summary>Gets the token associated with this operation scope.</summary>
    public override CancellationToken CancellationToken { get; }

    /// <inheritdoc />
    public ISession Session => _context.Session;
    /// <inheritdoc />
    public ConnectionContext ConnectionContext => _context.ConnectionContext;

    /// <inheritdoc />
    public Task<ITopic> GetTopicAsync(Topic topic, CancellationToken cancellationToken = default)
    {
        return _context.GetTopicAsync(topic, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public Task EnsureTopicExistsAsync(Topic topic, CancellationToken cancellationToken = default)
    {
        return _context.EnsureTopicExistsAsync(topic, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public Task<IQueue> GetQueueAsync(Queue queue, CancellationToken cancellationToken = default)
    {
        return _context.GetQueueAsync(queue, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public Task<IDestination> GetDestinationAsync(string destinationName, DestinationType destinationType, CancellationToken cancellationToken = default)
    {
        return _context.GetDestinationAsync(destinationName, destinationType, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public Task<IMessageConsumer> CreateMessageConsumerAsync(IDestination destination, string? selector, bool noLocal, string? consumerName = null,
        bool shared = false, bool durable = true, CancellationToken cancellationToken = default)
    {
        return _context.CreateMessageConsumerAsync(destination, selector, noLocal, consumerName, shared, durable, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public Task SendAsync(IDestination destination, IMessage message, CancellationToken cancellationToken)
    {
        return _context.SendAsync(destination, message, cancellationToken);
    }

    /// <inheritdoc />
    public IBytesMessage CreateBytesMessage(byte[] content)
    {
        return _context.CreateBytesMessage(content);
    }

    /// <inheritdoc />
    public Task DeleteTopicAsync(string topicName, CancellationToken cancellationToken = default)
    {
        return _context.DeleteTopicAsync(topicName, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public Task DeleteQueueAsync(string queueName, CancellationToken cancellationToken = default)
    {
        return _context.DeleteQueueAsync(queueName, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public IDestination? GetTemporaryDestination(string name)
    {
        return _context.GetTemporaryDestination(name);
    }

    /// <inheritdoc />
    public ITextMessage CreateTextMessage(string content)
    {
        return _context.CreateTextMessage(content);
    }

    /// <inheritdoc />
    public IMessage CreateMessage()
    {
        return _context.CreateMessage();
    }
}
