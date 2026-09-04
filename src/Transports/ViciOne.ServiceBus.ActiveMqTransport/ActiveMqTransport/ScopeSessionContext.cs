using System.Threading;
using System.Threading.Tasks;
using Apache.NMS;
using ViciOne.ServiceBus.ActiveMqTransport.Topology;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.ActiveMqTransport;

public class ScopeSessionContext :
    ScopePipeContext,
    SessionContext
{
    readonly SessionContext _context;

    public ScopeSessionContext(SessionContext context, CancellationToken cancellationToken)
        : base(context)
    {
        _context = context;
        CancellationToken = cancellationToken;
    }

    public override CancellationToken CancellationToken { get; }

    public ISession Session => _context.Session;
    public ConnectionContext ConnectionContext => _context.ConnectionContext;

    public Task<ITopic> GetTopicAsync(Topic topic, CancellationToken cancellationToken = default)
    {
        return _context.GetTopicAsync(topic, cancellationToken: cancellationToken);
    }

    public Task EnsureTopicExistsAsync(Topic topic, CancellationToken cancellationToken = default)
    {
        return _context.EnsureTopicExistsAsync(topic, cancellationToken: cancellationToken);
    }

    public Task<IQueue> GetQueueAsync(Queue queue, CancellationToken cancellationToken = default)
    {
        return _context.GetQueueAsync(queue, cancellationToken: cancellationToken);
    }

    public Task<IDestination> GetDestinationAsync(string destinationName, DestinationType destinationType, CancellationToken cancellationToken = default)
    {
        return _context.GetDestinationAsync(destinationName, destinationType, cancellationToken: cancellationToken);
    }

    public Task<IMessageConsumer> CreateMessageConsumerAsync(IDestination destination, string? selector, bool noLocal, string? consumerName = null,
        bool shared = false, bool durable = true, CancellationToken cancellationToken = default)
    {
        return _context.CreateMessageConsumerAsync(destination, selector, noLocal, consumerName, shared, durable, cancellationToken: cancellationToken);
    }

    public Task SendAsync(IDestination destination, IMessage message, CancellationToken cancellationToken)
    {
        return _context.SendAsync(destination, message, cancellationToken);
    }

    public IBytesMessage CreateBytesMessage(byte[] content)
    {
        return _context.CreateBytesMessage(content);
    }

    public Task DeleteTopicAsync(string topicName, CancellationToken cancellationToken = default)
    {
        return _context.DeleteTopicAsync(topicName, cancellationToken: cancellationToken);
    }

    public Task DeleteQueueAsync(string queueName, CancellationToken cancellationToken = default)
    {
        return _context.DeleteQueueAsync(queueName, cancellationToken: cancellationToken);
    }

    public IDestination? GetTemporaryDestination(string name)
    {
        return _context.GetTemporaryDestination(name);
    }

    public ITextMessage CreateTextMessage(string content)
    {
        return _context.CreateTextMessage(content);
    }

    public IMessage CreateMessage()
    {
        return _context.CreateMessage();
    }
}
