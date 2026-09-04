using System.Collections.Generic;
using ViciOne.ServiceBus.AmazonSqs.Topology;

namespace ViciOne.ServiceBus.AmazonSqs.Configuration;

/// <summary>
/// Provides an amazon sqs topic configurator implementation.
/// </summary>
public class AmazonSqsTopicConfigurator :
    EntityConfigurator,
    IAmazonSqsTopicConfigurator,
    Topic
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="topicName">The topic name value.</param>
    /// <param name="durable">The durable value.</param>
    /// <param name="autoDelete">The auto delete value.</param>
    /// <param name="topicAttributes">The topic attributes value.</param>
    /// <param name="topicSubscriptionAttributes">The topic subscription attributes value.</param>
    /// <param name="topicTags">The topic tags value.</param>
    public AmazonSqsTopicConfigurator(string topicName, bool durable = true, bool autoDelete = false, IDictionary<string, object>? topicAttributes = null,
        IDictionary<string, object>? topicSubscriptionAttributes = null, IDictionary<string, string>? topicTags = null)
        : base(topicName, durable, autoDelete)
    {
        TopicAttributes = topicAttributes ?? new Dictionary<string, object>();
        TopicSubscriptionAttributes = topicSubscriptionAttributes ?? new Dictionary<string, object>();
        TopicTags = topicTags ?? new Dictionary<string, string>();

        if (AmazonSqsEndpointAddress.IsFifo(topicName))
            TopicAttributes["FifoTopic"] = "true";
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="source">The source value.</param>
    public AmazonSqsTopicConfigurator(Topic source)
        : this(source.EntityName, source.Durable, source.AutoDelete, source.TopicAttributes, source.TopicSubscriptionAttributes, source.TopicTags)
    {
    }

    /// <summary>
    /// Gets the tags value.
    /// </summary>
    public IDictionary<string, string> Tags => TopicTags;

    /// <summary>
    /// Gets the address type value.
    /// </summary>
    protected override AmazonSqsEndpointAddress.AddressType AddressType => AmazonSqsEndpointAddress.AddressType.Topic;

    /// <summary>
    /// Gets the topic attributes value.
    /// </summary>
    public IDictionary<string, object> TopicAttributes { get; }
    /// <summary>
    /// Gets the topic subscription attributes value.
    /// </summary>
    public IDictionary<string, object> TopicSubscriptionAttributes { get; }
    /// <summary>
    /// Gets the topic tags value.
    /// </summary>
    public IDictionary<string, string> TopicTags { get; }
}
