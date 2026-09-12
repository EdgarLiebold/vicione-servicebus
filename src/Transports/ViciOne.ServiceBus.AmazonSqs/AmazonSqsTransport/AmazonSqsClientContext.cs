using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Provides operation-scoped access to Amazon SQS and Amazon SNS entities and messaging operations.</summary>
public class AmazonSqsClientContext :
    ScopePipeContext,
    ClientContext
{
    readonly IAmazonSimpleNotificationService _snsClient;
    readonly IAmazonSQS _sqsClient;

    /// <summary>Initializes an Amazon SQS client context.</summary>
    /// <param name="connectionContext">The connection that owns the entity caches used by this context.</param>
    /// <param name="sqsClient">The client used for Amazon SQS operations.</param>
    /// <param name="snsClient">The client used for Amazon SNS operations.</param>
    /// <param name="cancellationToken">The cancellation token associated with the context lifetime.</param>
    public AmazonSqsClientContext(ConnectionContext connectionContext,
        IAmazonSQS sqsClient,
        IAmazonSimpleNotificationService snsClient,
        CancellationToken cancellationToken)
        : base(connectionContext)
    {
        ConnectionContext = connectionContext;

        _sqsClient = sqsClient;
        _snsClient = snsClient;
        CancellationToken = cancellationToken;
    }

    /// <summary>Gets the cancellation token associated with the context lifetime.</summary>
    public override CancellationToken CancellationToken { get; }

    /// <summary>Gets the connection that owns the entity caches used by this context.</summary>
    public ConnectionContext ConnectionContext { get; }

    /// <summary>Gets or creates the Amazon SNS topic represented by the topology entity.</summary>
    /// <param name="topic">The topic topology entity.</param>
    /// <param name="cancellationToken">The token used to cancel entity resolution.</param>
    /// <returns>The resolved Amazon SNS topic information.</returns>
    public Task<TopicInfo> CreateTopicAsync(Topology.Topic topic, CancellationToken cancellationToken)
    {
        return ConnectionContext.GetTopicAsync(topic, cancellationToken);
    }

    /// <summary>Gets or creates the Amazon SQS queue represented by the topology entity.</summary>
    /// <param name="queue">The queue topology entity.</param>
    /// <param name="cancellationToken">The token used to cancel entity resolution.</param>
    /// <returns>The resolved Amazon SQS queue information.</returns>
    public Task<QueueInfo> CreateQueueAsync(Queue queue, CancellationToken cancellationToken)
    {
        return ConnectionContext.GetQueueAsync(queue, cancellationToken);
    }

    /// <summary>Creates or updates an Amazon SNS subscription from a topic to an Amazon SQS queue and grants the topic permission to send.</summary>
    /// <param name="topic">The source topic topology entity.</param>
    /// <param name="queue">The subscribed queue topology entity.</param>
    /// <param name="cancellationToken">The token used to cancel the subscription and policy operations.</param>
    /// <returns><see langword="true"/> when the queue policy was changed; <see langword="false"/> when permission already existed or no matching existing subscription could be resolved.</returns>
    public async Task<bool> CreateQueueSubscriptionAsync(Topology.Topic topic, Queue queue, CancellationToken cancellationToken)
    {
        var topicInfo = await ConnectionContext.GetTopicAsync(topic, cancellationToken).ConfigureAwait(false);
        var queueInfo = await ConnectionContext.GetQueueAsync(queue, cancellationToken).ConfigureAwait(false);

        Dictionary<string, string> subscriptionAttributes = topic.TopicSubscriptionAttributes.MergeLeft(queue.QueueSubscriptionAttributes)
            .ToDictionary(
                x => AmazonSqsAttributeDictionary.CanonicalizeSubscriptionAttributeName(x.Key),
                x => x.Value.ToString()!,
                StringComparer.OrdinalIgnoreCase);

        var subscribeRequest = new SubscribeRequest
        {
            TopicArn = topicInfo.Arn,
            Endpoint = queueInfo.Arn,
            Protocol = "sqs",
            Attributes = subscriptionAttributes
        };

        string? subscriptionArn = null;
        try
        {
            var response = await _snsClient.SubscribeAsync(subscribeRequest, cancellationToken).ConfigureAwait(false);

            response.EnsureSuccessfulResponse();

            subscriptionArn = response.SubscriptionArn;
        }
        catch (InvalidParameterException exception) when (exception.Message.Contains("exists"))
        {
            var existingSubscriptions = await _snsClient.ListSubscriptionsByTopicAsync(topicInfo.Arn, cancellationToken).ConfigureAwait(false);
            existingSubscriptions.EnsureSuccessfulResponse();

            var existingSubscription = existingSubscriptions.Subscriptions.SingleOrDefault(x =>
                x.TopicArn == topicInfo.Arn && x.Endpoint == queueInfo.Arn && x.Protocol == "sqs");

            if (existingSubscription != null)
            {
                subscriptionArn = existingSubscription.SubscriptionArn;
                var attributes = await _snsClient.GetSubscriptionAttributesAsync(subscriptionArn, cancellationToken)
                    .ConfigureAwait(false);

                if (attributes.HttpStatusCode is >= HttpStatusCode.OK and < HttpStatusCode.MultipleChoices)
                {
                    foreach (var (name, value) in SubscriptionAttributesEqual(attributes.Attributes, subscriptionAttributes))
                    {
                        var request = new SetSubscriptionAttributesRequest
                        {
                            AttributeName = name,
                            AttributeValue = value,
                            SubscriptionArn = subscriptionArn
                        };

                        var updated = await _snsClient.SetSubscriptionAttributesAsync(request, cancellationToken).ConfigureAwait(false);
                        updated.EnsureSuccessfulResponse();

                        LogContext.Debug?.Log("Updated subscription attribute: {SubscriptionArn} {Name}={Value}", subscriptionArn, name,
                            value);
                    }
                }
            }

            if (subscriptionArn == null)
                return false;
        }

        queueInfo.SubscriptionArns.Add(subscriptionArn);

        var sqsQueueArn = queueInfo.Arn;

        return await queueInfo.UpdatePolicyAsync(sqsQueueArn, topicInfo.Arn, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Deletes an Amazon SNS topic and evicts its cached entity information.</summary>
    /// <param name="topic">The topic topology entity to delete.</param>
    /// <param name="cancellationToken">The token used to cancel the delete operation.</param>
    /// <returns>A task that completes when the topic has been deleted and its cache entry removed.</returns>
    public async Task DeleteTopicAsync(Topology.Topic topic, CancellationToken cancellationToken)
    {
        var topicInfo = await ConnectionContext.GetTopicAsync(topic, cancellationToken).ConfigureAwait(false);

        TransportLogMessages.DeleteTopic(topicInfo.Arn);

        var response = await _snsClient.DeleteTopicAsync(topicInfo.Arn, cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessfulResponse();

        await ConnectionContext.RemoveTopicByNameAsync(topic.EntityName, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Deletes a queue's Amazon SNS subscriptions, deletes the Amazon SQS queue, and evicts its cached entity information.</summary>
    /// <param name="queue">The queue topology entity to delete.</param>
    /// <param name="cancellationToken">The token used to cancel the delete operations.</param>
    /// <returns>A task that completes when the subscriptions and queue have been deleted and the cache entry removed.</returns>
    public async Task DeleteQueueAsync(Queue queue, CancellationToken cancellationToken)
    {
        var queueInfo = await ConnectionContext.GetQueueAsync(queue, cancellationToken).ConfigureAwait(false);

        TransportLogMessages.DeleteQueue(queueInfo.Url);

        foreach (var subscriptionArn in queueInfo.SubscriptionArns)
        {
            TransportLogMessages.DeleteSubscription(queueInfo.Url, subscriptionArn);

            await DeleteQueueSubscriptionAsync(subscriptionArn, cancellationToken).ConfigureAwait(false);
        }

        var response = await _sqsClient.DeleteQueueAsync(queueInfo.Url, cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessfulResponse();

        await ConnectionContext.RemoveQueueByNameAsync(queue.EntityName, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Publishes a prepared batch entry through the named Amazon SNS topic.</summary>
    /// <param name="topicName">The logical topic name used to resolve the cached topic.</param>
    /// <param name="request">The Amazon SNS batch entry to publish.</param>
    /// <param name="cancellationToken">The token used to cancel topic resolution or publishing.</param>
    /// <returns>A task that completes when Amazon SNS accepts the batch entry.</returns>
    public async Task PublishAsync(string topicName, PublishBatchRequestEntry request, CancellationToken cancellationToken)
    {
        var topicInfo = await ConnectionContext.GetTopicByNameAsync(topicName, cancellationToken).ConfigureAwait(false);

        await topicInfo.PublishAsync(request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Sends a prepared batch entry to the named Amazon SQS queue.</summary>
    /// <param name="queueName">The logical queue name used to resolve the cached queue.</param>
    /// <param name="request">The Amazon SQS batch entry to send.</param>
    /// <param name="cancellationToken">The token used to cancel queue resolution or sending.</param>
    /// <returns>A task that completes when Amazon SQS accepts the batch entry.</returns>
    public async Task SendMessageAsync(string queueName, SendMessageBatchRequestEntry request, CancellationToken cancellationToken)
    {
        var queueInfo = await ConnectionContext.GetQueueByNameAsync(queueName, cancellationToken).ConfigureAwait(false);

        await queueInfo.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Deletes a received message from the named Amazon SQS queue.</summary>
    /// <param name="queueName">The logical queue name used to resolve the cached queue.</param>
    /// <param name="receiptHandle">The receipt handle returned by Amazon SQS for the received message.</param>
    /// <param name="cancellationToken">The token used to cancel queue resolution or deletion.</param>
    /// <returns>A task that completes when Amazon SQS accepts the delete request.</returns>
    public async Task DeleteMessageAsync(string queueName, string receiptHandle, CancellationToken cancellationToken)
    {
        var queueInfo = await ConnectionContext.GetQueueByNameAsync(queueName, cancellationToken).ConfigureAwait(false);

        await queueInfo.DeleteAsync(receiptHandle, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Requests removal of all available messages from the named Amazon SQS queue.</summary>
    /// <param name="queueName">The logical queue name used to resolve the cached queue.</param>
    /// <param name="cancellationToken">The token used to cancel queue resolution or the purge request.</param>
    /// <returns>A task that completes when Amazon SQS accepts the purge request.</returns>
    public async Task PurgeQueueAsync(string queueName, CancellationToken cancellationToken)
    {
        var queueInfo = await ConnectionContext.GetQueueByNameAsync(queueName, cancellationToken).ConfigureAwait(false);

        var response = await _sqsClient.PurgeQueueAsync(queueInfo.Url, cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessfulResponse();
    }

    /// <summary>Receives messages and all message attributes from the named Amazon SQS queue.</summary>
    /// <param name="queueName">The logical queue name used to resolve the cached queue.</param>
    /// <param name="messageLimit">The maximum number of messages requested from Amazon SQS.</param>
    /// <param name="waitTime">The long-poll wait time, in seconds.</param>
    /// <param name="cancellationToken">The token used to cancel queue resolution or receiving.</param>
    /// <returns>The messages returned by Amazon SQS, or an empty list when the response does not contain a message collection.</returns>
    public async Task<IList<Message>> ReceiveMessagesAsync(string queueName, int messageLimit, int waitTime, CancellationToken cancellationToken)
    {
        var queueInfo = await ConnectionContext.GetQueueByNameAsync(queueName, cancellationToken).ConfigureAwait(false);

        var request = new ReceiveMessageRequest(queueInfo.Url)
        {
            MaxNumberOfMessages = messageLimit,
            WaitTimeSeconds = waitTime,
            MessageSystemAttributeNames = ["All"],
            MessageAttributeNames = ["All"]
        };

        var response = await _sqsClient.ReceiveMessageAsync(request, cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessfulResponse();

        return response.Messages ?? new List<Message>();
    }

    /// <summary>Resolves cached entity information for a named Amazon SQS queue.</summary>
    /// <param name="queueName">The logical queue name.</param>
    /// <param name="cancellationToken">The token used to cancel queue resolution.</param>
    /// <returns>The resolved Amazon SQS queue information.</returns>
    public Task<QueueInfo> GetQueueInfoAsync(string queueName, CancellationToken cancellationToken)
    {
        return ConnectionContext.GetQueueByNameAsync(queueName, cancellationToken);
    }

    /// <summary>Changes the visibility timeout of a received Amazon SQS message.</summary>
    /// <param name="queueUrl">The Amazon SQS queue URL.</param>
    /// <param name="receiptHandle">The receipt handle returned for the received message.</param>
    /// <param name="seconds">The new visibility timeout, in seconds.</param>
    /// <param name="cancellationToken">The token used to cancel the request.</param>
    /// <returns>A task that completes when Amazon SQS accepts the visibility change.</returns>
    public async Task ChangeMessageVisibilityAsync(string queueUrl, string receiptHandle, int seconds, CancellationToken cancellationToken)
    {
        var response = await _sqsClient.ChangeMessageVisibilityAsync(new ChangeMessageVisibilityRequest
        {
            QueueUrl = queueUrl,
            ReceiptHandle = receiptHandle,
            VisibilityTimeout = seconds
        }, cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessfulResponse();
    }

    async Task DeleteQueueSubscriptionAsync(string subscriptionArn, CancellationToken cancellationToken)
    {
        var unsubscribeRequest = new UnsubscribeRequest { SubscriptionArn = subscriptionArn };

        var response = await _snsClient.UnsubscribeAsync(unsubscribeRequest, cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessfulResponse();
    }

    static IEnumerable<(string, string)> SubscriptionAttributesEqual(Dictionary<string, string> existingAttributes,
        Dictionary<string, string> updatedAttributes)
    {
        if (updatedAttributes.TryGetValue("FilterPolicy", out var filterPolicy))
        {
            if (!existingAttributes.TryGetValue("FilterPolicy", out var existingFilterPolicy) || existingFilterPolicy != filterPolicy)
                yield return ("FilterPolicy", filterPolicy);
        }

        if (updatedAttributes.TryGetValue("FilterPolicyScope", out var filterPolicyScope))
        {
            if (!existingAttributes.TryGetValue("FilterPolicyScope", out var existingFilterPolicyScope) || existingFilterPolicyScope != filterPolicyScope)
                yield return ("FilterPolicyScope", filterPolicyScope);
        }

        if (updatedAttributes.TryGetValue("RawMessageDelivery", out var rawMessageDelivery))
        {
            if (!existingAttributes.TryGetValue("RawMessageDelivery", out var existingRawMessageDelivery)
                || existingRawMessageDelivery != rawMessageDelivery)
                yield return ("RawMessageDelivery", rawMessageDelivery);
        }
    }
}
