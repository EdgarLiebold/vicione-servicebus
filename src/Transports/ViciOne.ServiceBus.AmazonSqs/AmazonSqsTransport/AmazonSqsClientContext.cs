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

/// <summary>
/// Provides an amazon sqs client context implementation.
/// </summary>
public class AmazonSqsClientContext :
    ScopePipeContext,
    ClientContext
{
    readonly IAmazonSimpleNotificationService _snsClient;
    readonly IAmazonSQS _sqsClient;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="connectionContext">The connection context value.</param>
    /// <param name="sqsClient">The sqs client value.</param>
    /// <param name="snsClient">The sns client value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
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

    /// <summary>
    /// Gets the cancellation token value.
    /// </summary>
    public override CancellationToken CancellationToken { get; }

    /// <summary>
    /// Gets the connection context value.
    /// </summary>
    public ConnectionContext ConnectionContext { get; }

    /// <summary>
    /// Creates topic.
    /// </summary>
    /// <param name="topic">The topic value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<TopicInfo> CreateTopicAsync(Topology.Topic topic, CancellationToken cancellationToken)
    {
        return ConnectionContext.GetTopicAsync(topic, cancellationToken);
    }

    /// <summary>
    /// Creates queue.
    /// </summary>
    /// <param name="queue">The queue value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<QueueInfo> CreateQueueAsync(Queue queue, CancellationToken cancellationToken)
    {
        return ConnectionContext.GetQueueAsync(queue, cancellationToken);
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
        var topicInfo = await ConnectionContext.GetTopicAsync(topic, cancellationToken).ConfigureAwait(false);
        var queueInfo = await ConnectionContext.GetQueueAsync(queue, cancellationToken).ConfigureAwait(false);

        Dictionary<string, string> subscriptionAttributes = topic.TopicSubscriptionAttributes.MergeLeft(queue.QueueSubscriptionAttributes)
            .ToDictionary(x => x.Key, x => x.Value.ToString()!);

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

    /// <summary>
    /// Performs the delete topic operation.
    /// </summary>
    /// <param name="topic">The topic value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task DeleteTopicAsync(Topology.Topic topic, CancellationToken cancellationToken)
    {
        var topicInfo = await ConnectionContext.GetTopicAsync(topic, cancellationToken).ConfigureAwait(false);

        TransportLogMessages.DeleteTopic(topicInfo.Arn);

        var response = await _snsClient.DeleteTopicAsync(topicInfo.Arn, cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessfulResponse();

        await ConnectionContext.RemoveTopicByNameAsync(topic.EntityName, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the delete queue operation.
    /// </summary>
    /// <param name="queue">The queue value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Publishes a message to its configured consumers.
    /// </summary>
    /// <param name="topicName">The topic name value.</param>
    /// <param name="request">The request value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task PublishAsync(string topicName, PublishBatchRequestEntry request, CancellationToken cancellationToken)
    {
        var topicInfo = await ConnectionContext.GetTopicByNameAsync(topicName, cancellationToken).ConfigureAwait(false);

        await topicInfo.PublishAsync(request, cancellationToken).ConfigureAwait(false);
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
        var queueInfo = await ConnectionContext.GetQueueByNameAsync(queueName, cancellationToken).ConfigureAwait(false);

        await queueInfo.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the delete message operation.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="receiptHandle">The receipt handle value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task DeleteMessageAsync(string queueName, string receiptHandle, CancellationToken cancellationToken)
    {
        var queueInfo = await ConnectionContext.GetQueueByNameAsync(queueName, cancellationToken).ConfigureAwait(false);

        await queueInfo.DeleteAsync(receiptHandle, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the purge queue operation.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task PurgeQueueAsync(string queueName, CancellationToken cancellationToken)
    {
        var queueInfo = await ConnectionContext.GetQueueByNameAsync(queueName, cancellationToken).ConfigureAwait(false);

        var response = await _sqsClient.PurgeQueueAsync(queueInfo.Url, cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessfulResponse();
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

    /// <summary>
    /// Gets queue info.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<QueueInfo> GetQueueInfoAsync(string queueName, CancellationToken cancellationToken)
    {
        return ConnectionContext.GetQueueByNameAsync(queueName, cancellationToken);
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
