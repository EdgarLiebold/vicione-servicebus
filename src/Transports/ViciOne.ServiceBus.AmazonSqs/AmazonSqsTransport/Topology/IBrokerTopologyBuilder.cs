using System.Collections.Generic;

namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>Creates and links Amazon SNS topic, Amazon SQS queue, and subscription topology entities.</summary>
public interface IBrokerTopologyBuilder
{
    /// <summary>Declares an Amazon SNS topic.</summary>
    /// <param name="name">The topic name.</param>
    /// <param name="durable">Whether the topic is retained beyond the bus lifetime.</param>
    /// <param name="autoDelete">Whether the transport deletes the topic when the bus stops.</param>
    /// <param name="topicAttributes">Optional Amazon SNS topic attributes.</param>
    /// <param name="topicSubscriptionAttributes">Optional default subscription attributes.</param>
    /// <param name="tags">Optional topic tags.</param>
    /// <returns>An entity handle used to reference the topic in subsequent calls.</returns>
    TopicHandle CreateTopic(string name, bool durable, bool autoDelete, IDictionary<string, object>? topicAttributes = null,
        IDictionary<string, object>? topicSubscriptionAttributes = null, IDictionary<string, string>? tags = null);

    /// <summary>Declares an Amazon SQS queue.</summary>
    /// <param name="name">The queue name.</param>
    /// <param name="durable">Whether the queue is retained beyond the bus lifetime.</param>
    /// <param name="autoDelete">Whether the transport deletes the queue when the bus stops.</param>
    /// <param name="queueAttributes">Optional Amazon SQS queue attributes.</param>
    /// <param name="queueSubscriptionAttributes">Optional attributes for subscriptions targeting the queue.</param>
    /// <param name="tags">Optional queue tags.</param>
    /// <returns>An entity handle used to reference the queue in subsequent calls.</returns>
    QueueHandle CreateQueue(string name, bool durable, bool autoDelete, IDictionary<string, object>? queueAttributes = null,
        IDictionary<string, object>? queueSubscriptionAttributes = null, IDictionary<string, string>? tags = null);

    /// <summary>Declares a subscription from an Amazon SNS topic to an Amazon SQS queue.</summary>
    /// <param name="topic">The source topic handle.</param>
    /// <param name="queue">The destination queue handle.</param>
    /// <returns>A handle to the declared subscription.</returns>
    QueueSubscriptionHandle CreateQueueSubscription(TopicHandle topic, QueueHandle queue);

}
