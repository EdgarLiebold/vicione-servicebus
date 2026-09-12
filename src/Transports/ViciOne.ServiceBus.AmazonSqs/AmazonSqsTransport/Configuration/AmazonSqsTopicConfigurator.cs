using System.Collections.Generic;
using ViciOne.ServiceBus.AmazonSqs.Topology;

namespace ViciOne.ServiceBus.AmazonSqs.Configuration;

/// <summary>Configures an Amazon SNS topic and subscriptions originating from it.</summary>
public class AmazonSqsTopicConfigurator :
    EntityConfigurator,
    IAmazonSqsTopicConfigurator,
    Topic
{
    /// <summary>Initializes topic configuration and marks <c>.fifo</c> topics as FIFO entities.</summary>
    /// <param name="topicName">The Amazon SNS topic name.</param>
    /// <param name="durable">Whether the topic is retained when its endpoint stops.</param>
    /// <param name="autoDelete">Whether the topic is deleted when its endpoint stops.</param>
    /// <param name="topicAttributes">Optional Amazon SNS topic attributes.</param>
    /// <param name="topicSubscriptionAttributes">Optional default Amazon SNS subscription attributes.</param>
    /// <param name="topicTags">Optional tags applied to the topic.</param>
    public AmazonSqsTopicConfigurator(string topicName, bool durable = true, bool autoDelete = false, IDictionary<string, object>? topicAttributes = null,
        IDictionary<string, object>? topicSubscriptionAttributes = null, IDictionary<string, string>? topicTags = null)
        : base(topicName, durable, autoDelete)
    {
        TopicAttributes = topicAttributes ?? new Dictionary<string, object>();
        TopicSubscriptionAttributes = AmazonSqsAttributeDictionary.CopySubscriptionAttributes(topicSubscriptionAttributes);
        TopicTags = topicTags ?? new Dictionary<string, string>();

        if (AmazonSqsEndpointAddress.IsFifo(topicName))
            TopicAttributes["FifoTopic"] = "true";
    }

    /// <summary>Initializes topic configuration from an existing topology entity.</summary>
    /// <param name="source">The topic topology entity whose settings initialize this configurator.</param>
    public AmazonSqsTopicConfigurator(Topic source)
        : this(source.EntityName, source.Durable, source.AutoDelete, source.TopicAttributes, source.TopicSubscriptionAttributes, source.TopicTags)
    {
    }

    /// <summary>Gets the tags applied to the topic.</summary>
    public IDictionary<string, string> Tags => TopicTags;

    /// <summary>Gets the topic address type used for endpoint-address formatting.</summary>
    protected override AmazonSqsEndpointAddress.AddressType AddressType => AmazonSqsEndpointAddress.AddressType.Topic;

    /// <summary>Gets the Amazon SNS topic attributes.</summary>
    public IDictionary<string, object> TopicAttributes { get; }
    /// <summary>Gets the default Amazon SNS subscription attributes.</summary>
    public IDictionary<string, object> TopicSubscriptionAttributes { get; }
    /// <summary>Gets the tags applied to the topic.</summary>
    public IDictionary<string, string> TopicTags { get; }
}
