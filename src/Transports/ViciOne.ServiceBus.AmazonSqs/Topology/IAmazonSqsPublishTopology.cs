using System.Collections.Generic;
using ViciOne.ServiceBus.AmazonSqs.Topology;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Defines the contract for amazon sqs publish topology.
/// </summary>
public interface IAmazonSqsPublishTopology :
    IPublishTopology
{
    /// <summary>
    /// Additional <see href="https://docs.aws.amazon.com/sns/latest/api/API_SetTopicAttributes.html">attributes</see> for the topic.
    /// </summary>
    IDictionary<string, object> TopicAttributes { get; }

    /// <summary>
    /// Additional <see href="https://docs.aws.amazon.com/sns/latest/api/API_SetSubscriptionAttributes.html">attributes</see> for the topic's subscription.
    /// </summary>
    IDictionary<string, object> TopicSubscriptionAttributes { get; }

    /// <summary>
    /// Collection of tags to assign to topic when created.
    /// </summary>
    IDictionary<string, string> TopicTags { get; }

    /// <summary>
    /// Gets message topology.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    new IAmazonSqsMessagePublishTopology<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>
    /// Gets publish broker topology.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    BrokerTopology GetPublishBrokerTopology();
}
