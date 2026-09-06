using System.Collections.Generic;
using ViciOne.ServiceBus.AmazonSqs.Topology;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Defines Amazon SNS publish topology, defaults, and message-specific topics.</summary>
public interface IAmazonSqsPublishTopology :
    IPublishTopology
{
    /// <summary>Gets default <see href="https://docs.aws.amazon.com/sns/latest/api/API_SetTopicAttributes.html">Amazon SNS topic attributes</see>.</summary>
    IDictionary<string, object> TopicAttributes { get; }

    /// <summary>Gets default <see href="https://docs.aws.amazon.com/sns/latest/api/API_SetSubscriptionAttributes.html">Amazon SNS subscription attributes</see>.</summary>
    IDictionary<string, object> TopicSubscriptionAttributes { get; }

    /// <summary>Gets default tags assigned when a topic is created.</summary>
    IDictionary<string, string> TopicTags { get; }

    /// <summary>Gets publish topology for a message type.</summary>
    /// <typeparam name="T">The published message type.</typeparam>
    /// <returns>The typed Amazon SNS message publish topology.</returns>
    new IAmazonSqsMessagePublishTopology<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>Builds combined broker topology for all configured publish message types.</summary>
    /// <returns>The aggregate Amazon SNS publish topology.</returns>
    BrokerTopology GetPublishBrokerTopology();
}
