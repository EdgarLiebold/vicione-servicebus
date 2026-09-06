using System.Collections.Generic;

namespace ViciOne.ServiceBus.AmazonSqs.Topology;
/// <summary>Describes an Amazon SNS topic declaration.</summary>
public interface Topic
{
    /// <summary>Gets the topic name.</summary>
    string EntityName { get; }

    /// <summary>Gets whether the topic is retained beyond the bus lifetime.</summary>
    bool Durable { get; }

    /// <summary>Gets whether the transport deletes the topic when the bus stops.</summary>
    bool AutoDelete { get; }

    /// <summary>Gets additional <see href="https://docs.aws.amazon.com/sns/latest/api/API_SetTopicAttributes.html">Amazon SNS topic attributes</see>.</summary>
    IDictionary<string, object> TopicAttributes { get; }

    /// <summary>Gets additional default <see href="https://docs.aws.amazon.com/sns/latest/api/API_SetSubscriptionAttributes.html">Amazon SNS subscription attributes</see>.</summary>
    IDictionary<string, object> TopicSubscriptionAttributes { get; }

    /// <summary>Gets the tags assigned when the topic is created.</summary>
    IDictionary<string, string> TopicTags { get; }
}
