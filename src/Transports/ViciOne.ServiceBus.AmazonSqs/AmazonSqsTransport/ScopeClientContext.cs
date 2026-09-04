using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS.Model;
using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Provides a scope client context implementation.
/// </summary>
public class ScopeClientContext :
    ScopePipeContext,
    ClientContext,
    IDisposable
{
    readonly CancellationToken _cancellationToken;
    readonly ClientContext _context;
    CancellationTokenSource? _tokenSource;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public ScopeClientContext(ClientContext context, CancellationToken cancellationToken)
        : base(context)
    {
        _context = context;

        _cancellationToken = cancellationToken;
        _tokenSource = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken, cancellationToken);
    }

    /// <summary>
    /// Gets the cancellation token value.
    /// </summary>
    public override CancellationToken CancellationToken => _tokenSource?.Token ?? _cancellationToken;

    /// <summary>
    /// Gets the connection context value.
    /// </summary>
    public ConnectionContext ConnectionContext => _context.ConnectionContext;

    /// <summary>
    /// Creates topic.
    /// </summary>
    /// <param name="topic">The topic value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<TopicInfo> CreateTopicAsync(Topology.Topic topic, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _context.CreateTopicAsync(topic, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates queue.
    /// </summary>
    /// <param name="queue">The queue value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<QueueInfo> CreateQueueAsync(Queue queue, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _context.CreateQueueAsync(queue, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates queue subscription.
    /// </summary>
    /// <param name="topic">The topic value.</param>
    /// <param name="queue">The queue value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<bool> CreateQueueSubscriptionAsync(Topology.Topic topic, Queue queue, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _context.CreateQueueSubscriptionAsync(topic, queue, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the delete topic operation.
    /// </summary>
    /// <param name="topic">The topic value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task DeleteTopicAsync(Topology.Topic topic, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.DeleteTopicAsync(topic, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the delete queue operation.
    /// </summary>
    /// <param name="queue">The queue value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task DeleteQueueAsync(Queue queue, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.DeleteQueueAsync(queue, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>
    /// Publishes a message to its configured consumers.
    /// </summary>
    /// <param name="topicName">The topic name value.</param>
    /// <param name="request">The request value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task PublishAsync(string topicName, PublishBatchRequestEntry request, CancellationToken cancellationToken = default)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.PublishAsync(topicName, request, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends message.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="request">The request value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task SendMessageAsync(string queueName, SendMessageBatchRequestEntry request, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.SendMessageAsync(queueName, request, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the delete message operation.
    /// </summary>
    /// <param name="queueUrl">The queue url value.</param>
    /// <param name="receiptHandle">The receipt handle value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task DeleteMessageAsync(string queueUrl, string receiptHandle, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.DeleteMessageAsync(queueUrl, receiptHandle, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the purge queue operation.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task PurgeQueueAsync(string queueName, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.PurgeQueueAsync(queueName, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the receive messages operation.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="messageLimit">The message limit value.</param>
    /// <param name="waitTime">The wait time value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<IList<Message>> ReceiveMessagesAsync(string queueName, int messageLimit, int waitTime, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _context.ReceiveMessagesAsync(queueName, messageLimit, waitTime, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets queue info.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<QueueInfo> GetQueueInfoAsync(string queueName, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        return await _context.GetQueueInfoAsync(queueName, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the change message visibility operation.
    /// </summary>
    /// <param name="queueUrl">The queue url value.</param>
    /// <param name="receiptHandle">The receipt handle value.</param>
    /// <param name="seconds">The seconds value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task ChangeMessageVisibilityAsync(string queueUrl, string receiptHandle, int seconds, CancellationToken cancellationToken)
    {
        using var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

        await _context.ChangeMessageVisibilityAsync(queueUrl, receiptHandle, seconds, tokenSource.Token).ConfigureAwait(false);
    }

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    public void Dispose()
    {
        _tokenSource?.Dispose();
        _tokenSource = null;
    }
}
