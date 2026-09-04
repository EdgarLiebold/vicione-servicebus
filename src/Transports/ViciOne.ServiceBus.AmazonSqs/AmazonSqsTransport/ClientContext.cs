using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS.Model;
using ViciOne.ServiceBus.AmazonSqs.Topology;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Defines the contract for client context.
/// </summary>
public interface ClientContext :
    PipeContext
{
    /// <summary>
    /// Gets the connection context value.
    /// </summary>
    ConnectionContext ConnectionContext { get; }

    /// <summary>
    /// Creates topic.
    /// </summary>
    /// <param name="topic">The topic value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<TopicInfo> CreateTopicAsync(Topology.Topic topic, CancellationToken cancellationToken);

    /// <summary>
    /// Creates queue.
    /// </summary>
    /// <param name="queue">The queue value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<QueueInfo> CreateQueueAsync(Queue queue, CancellationToken cancellationToken);

    /// <summary>
    /// Creates queue subscription.
    /// </summary>
    /// <param name="topic">The topic value.</param>
    /// <param name="queue">The queue value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<bool> CreateQueueSubscriptionAsync(Topology.Topic topic, Queue queue, CancellationToken cancellationToken);

    /// <summary>
    /// Performs the delete topic operation.
    /// </summary>
    /// <param name="topic">The topic value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task DeleteTopicAsync(Topology.Topic topic, CancellationToken cancellationToken);

    /// <summary>
    /// Performs the delete queue operation.
    /// </summary>
    /// <param name="queue">The queue value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task DeleteQueueAsync(Queue queue, CancellationToken cancellationToken);

    /// <summary>
    /// Publishes a message to its configured consumers.
    /// </summary>
    /// <param name="topicName">The topic name value.</param>
    /// <param name="request">The request value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task PublishAsync(string topicName, PublishBatchRequestEntry request, CancellationToken cancellationToken);

    /// <summary>
    /// Sends message.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="request">The request value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task SendMessageAsync(string queueName, SendMessageBatchRequestEntry request, CancellationToken cancellationToken);

    /// <summary>
    /// Performs the delete message operation.
    /// </summary>
    /// <param name="queueUrl">The queue url value.</param>
    /// <param name="receiptHandle">The receipt handle value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task DeleteMessageAsync(string queueUrl, string receiptHandle, CancellationToken cancellationToken);

    /// <summary>
    /// Performs the purge queue operation.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task PurgeQueueAsync(string queueName, CancellationToken cancellationToken);

    /// <summary>
    /// Performs the receive messages operation.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="messageLimit">The message limit value.</param>
    /// <param name="waitTime">The wait time value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<IList<Message>> ReceiveMessagesAsync(string queueName, int messageLimit, int waitTime, CancellationToken cancellationToken);

    /// <summary>
    /// Gets queue info.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<QueueInfo> GetQueueInfoAsync(string queueName, CancellationToken cancellationToken);

    /// <summary>
    /// Performs the change message visibility operation.
    /// </summary>
    /// <param name="queueUrl">The queue url value.</param>
    /// <param name="receiptHandle">The receipt handle value.</param>
    /// <param name="seconds">The seconds value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task ChangeMessageVisibilityAsync(string queueUrl, string receiptHandle, int seconds, CancellationToken cancellationToken);
}
