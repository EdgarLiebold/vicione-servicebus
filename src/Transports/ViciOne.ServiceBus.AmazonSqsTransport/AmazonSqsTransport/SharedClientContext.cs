using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS.Model;
using ViciOne.ServiceBus.AmazonSqsTransport.Topology;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.AmazonSqsTransport;

public class SharedClientContext :
    ProxyPipeContext,
    ClientContext
{
    readonly ClientContext _context;

    public SharedClientContext(ClientContext context, CancellationToken cancellationToken)
        : base(context)
    {
        _context = context;

        CancellationToken = cancellationToken;
    }

    public override CancellationToken CancellationToken { get; }

    public ConnectionContext ConnectionContext => _context.ConnectionContext;

    public async Task<TopicInfo> CreateTopicAsync(Topology.Topic topic, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _context.CreateTopicAsync(topic, tokenSource.Token).ConfigureAwait(false);
    }

    public async Task<QueueInfo> CreateQueueAsync(Queue queue, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _context.CreateQueueAsync(queue, tokenSource.Token).ConfigureAwait(false);
    }

    public async Task<bool> CreateQueueSubscriptionAsync(Topology.Topic topic, Queue queue, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _context.CreateQueueSubscriptionAsync(topic, queue, tokenSource.Token).ConfigureAwait(false);
    }

    public async Task DeleteTopicAsync(Topology.Topic topic, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.DeleteTopicAsync(topic, tokenSource.Token).ConfigureAwait(false);
    }

    public async Task DeleteQueueAsync(Queue queue, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.DeleteQueueAsync(queue, tokenSource.Token).ConfigureAwait(false);
    }

    public async Task PublishAsync(string topicName, PublishBatchRequestEntry request, CancellationToken cancellationToken = default)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.PublishAsync(topicName, request, tokenSource.Token).ConfigureAwait(false);
    }

    public async Task SendMessageAsync(string queueName, SendMessageBatchRequestEntry request, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.SendMessageAsync(queueName, request, tokenSource.Token).ConfigureAwait(false);
    }

    public async Task DeleteMessageAsync(string queueUrl, string receiptHandle, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.DeleteMessageAsync(queueUrl, receiptHandle, tokenSource.Token).ConfigureAwait(false);
    }

    public async Task PurgeQueueAsync(string queueName, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.PurgeQueueAsync(queueName, tokenSource.Token).ConfigureAwait(false);
    }

    public async Task<IList<Message>> ReceiveMessagesAsync(string queueName, int messageLimit, int waitTime, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _context.ReceiveMessagesAsync(queueName, messageLimit, waitTime, tokenSource.Token).ConfigureAwait(false);
    }

    public async Task<QueueInfo> GetQueueInfoAsync(string queueName, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _context.GetQueueInfoAsync(queueName, tokenSource.Token).ConfigureAwait(false);
    }

    public async Task ChangeMessageVisibilityAsync(string queueUrl, string receiptHandle, int seconds, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.ChangeMessageVisibilityAsync(queueUrl, receiptHandle, seconds, tokenSource.Token).ConfigureAwait(false);
    }
}
