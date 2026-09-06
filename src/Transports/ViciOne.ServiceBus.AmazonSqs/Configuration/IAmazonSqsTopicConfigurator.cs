using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.AmazonSqs;
/// <summary>Configures an Amazon SNS topic used by the transport.</summary>
public interface IAmazonSqsTopicConfigurator
{
    /// <summary>Sets whether the topic is retained when its endpoint stops.</summary>
    bool Durable { set; }

    /// <summary>Sets whether the transport deletes the topic when its endpoint stops.</summary>
    bool AutoDelete { set; }

    /// <summary>Gets optional <see href="https://docs.aws.amazon.com/sns/latest/api/API_SetTopicAttributes.html">Amazon SNS topic attributes</see>.</summary>
    IDictionary<string, object> TopicAttributes { get; }

    /// <summary>Gets optional default <see href="https://docs.aws.amazon.com/sns/latest/api/API_SetSubscriptionAttributes.html">Amazon SNS subscription attributes</see>.</summary>
    IDictionary<string, object> TopicSubscriptionAttributes { get; }

    /// <summary>Gets the tags assigned when the topic is created.</summary>
    IDictionary<string, string> TopicTags { get; }

    /// <summary>Formats the topic endpoint address relative to an Amazon SQS host.</summary>
    /// <param name="hostAddress">The Amazon SQS host address.</param>
    /// <returns>The Amazon SNS topic endpoint address.</returns>
    AmazonSqsEndpointAddress GetEndpointAddress(Uri hostAddress);
}
