using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS.Model;
using ViciOne.ServiceBus.AmazonSqs.Topology;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Provides Amazon SQS and Amazon SNS entity and messaging operations within a pipe context.</summary>
public interface ClientContext :
    PipeContext
{
    /// <summary>Gets the connection that owns the Amazon entity caches.</summary>
    ConnectionContext ConnectionContext { get; }

    /// <summary>Gets or creates the Amazon SNS topic represented by a topology entity.</summary>
    /// <param name="topic">The topic topology entity.</param>
    /// <param name="cancellationToken">The token used to cancel topic resolution.</param>
    /// <returns>The resolved topic information.</returns>
    Task<TopicInfo> CreateTopicAsync(Topology.Topic topic, CancellationToken cancellationToken);

    /// <summary>Gets or creates the Amazon SQS queue represented by a topology entity.</summary>
    /// <param name="queue">The queue topology entity.</param>
    /// <param name="cancellationToken">The token used to cancel queue resolution.</param>
    /// <returns>The resolved queue information.</returns>
    Task<QueueInfo> CreateQueueAsync(Queue queue, CancellationToken cancellationToken);

    /// <summary>Creates or updates an Amazon SNS subscription from a topic to an Amazon SQS queue.</summary>
    /// <param name="topic">The source topic topology entity.</param>
    /// <param name="queue">The subscribed queue topology entity.</param>
    /// <param name="cancellationToken">The token used to cancel subscription creation.</param>
    /// <returns><see langword="true"/> when the queue policy was changed; <see langword="false"/> when permission already existed or no matching existing subscription could be resolved.</returns>
    Task<bool> CreateQueueSubscriptionAsync(Topology.Topic topic, Queue queue, CancellationToken cancellationToken);

    /// <summary>Deletes an Amazon SNS topic and removes its cached entity information.</summary>
    /// <param name="topic">The topic topology entity.</param>
    /// <param name="cancellationToken">The token used to cancel deletion.</param>
    /// <returns>A task that completes when deletion and cache eviction finish.</returns>
    Task DeleteTopicAsync(Topology.Topic topic, CancellationToken cancellationToken);

    /// <summary>Deletes an Amazon SQS queue, its Amazon SNS subscriptions, and its cached entity information.</summary>
    /// <param name="queue">The queue topology entity.</param>
    /// <param name="cancellationToken">The token used to cancel deletion.</param>
    /// <returns>A task that completes when deletion and cache eviction finish.</returns>
    Task DeleteQueueAsync(Queue queue, CancellationToken cancellationToken);

    /// <summary>Publishes a prepared batch entry through a named Amazon SNS topic.</summary>
    /// <param name="topicName">The logical topic name.</param>
    /// <param name="request">The Amazon SNS batch entry.</param>
    /// <param name="cancellationToken">The token used to cancel publishing.</param>
    /// <returns>A task that completes when Amazon SNS accepts the entry.</returns>
    Task PublishAsync(string topicName, PublishBatchRequestEntry request, CancellationToken cancellationToken);

    /// <summary>Sends a prepared batch entry to a named Amazon SQS queue.</summary>
    /// <param name="queueName">The logical queue name.</param>
    /// <param name="request">The Amazon SQS batch entry.</param>
    /// <param name="cancellationToken">The token used to cancel sending.</param>
    /// <returns>A task that completes when Amazon SQS accepts the entry.</returns>
    Task SendMessageAsync(string queueName, SendMessageBatchRequestEntry request, CancellationToken cancellationToken);

    /// <summary>Deletes a received message from an Amazon SQS queue.</summary>
    /// <param name="queueName">The logical queue name used to resolve the queue.</param>
    /// <param name="receiptHandle">The receipt handle returned for the received message.</param>
    /// <param name="cancellationToken">The token used to cancel deletion.</param>
    /// <returns>A task that completes when Amazon SQS accepts the delete request.</returns>
    Task DeleteMessageAsync(string queueName, string receiptHandle, CancellationToken cancellationToken);

    /// <summary>Requests removal of all available messages from a named Amazon SQS queue.</summary>
    /// <param name="queueName">The logical queue name.</param>
    /// <param name="cancellationToken">The token used to cancel the purge request.</param>
    /// <returns>A task that completes when Amazon SQS accepts the request.</returns>
    Task PurgeQueueAsync(string queueName, CancellationToken cancellationToken);

    /// <summary>Receives messages and all attributes from a named Amazon SQS queue.</summary>
    /// <param name="queueName">The logical queue name.</param>
    /// <param name="messageLimit">The maximum number of messages requested.</param>
    /// <param name="waitTime">The long-poll wait time, in seconds.</param>
    /// <param name="cancellationToken">The token used to cancel receiving.</param>
    /// <returns>The messages returned by Amazon SQS.</returns>
    Task<IList<Message>> ReceiveMessagesAsync(string queueName, int messageLimit, int waitTime, CancellationToken cancellationToken);

    /// <summary>Resolves entity information for a named Amazon SQS queue.</summary>
    /// <param name="queueName">The logical queue name.</param>
    /// <param name="cancellationToken">The token used to cancel queue resolution.</param>
    /// <returns>The resolved queue information.</returns>
    Task<QueueInfo> GetQueueInfoAsync(string queueName, CancellationToken cancellationToken);

    /// <summary>Changes the visibility timeout of a received Amazon SQS message.</summary>
    /// <param name="queueUrl">The Amazon SQS queue URL.</param>
    /// <param name="receiptHandle">The receipt handle returned for the received message.</param>
    /// <param name="seconds">The new visibility timeout, in seconds.</param>
    /// <param name="cancellationToken">The token used to cancel the request.</param>
    /// <returns>A task that completes when Amazon SQS accepts the visibility change.</returns>
    Task ChangeMessageVisibilityAsync(string queueUrl, string receiptHandle, int seconds, CancellationToken cancellationToken);
}
