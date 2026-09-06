using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.AmazonSqs.Configuration;

namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>Represents Amazon SNS topic settings for a publish destination.</summary>
public class TopicPublishSettings :
    AmazonSqsTopicConfigurator,
    PublishSettings
{
    /// <summary>Initializes topic publish settings from an endpoint address.</summary>
    /// <param name="address">The Amazon SNS topic endpoint address.</param>
    public TopicPublishSettings(AmazonSqsEndpointAddress address)
        : base(address.Name, address.Durable, address.AutoDelete)
    {
    }

    /// <summary>Formats the topic send address relative to an Amazon SQS host.</summary>
    /// <param name="hostAddress">The Amazon SQS host address.</param>
    /// <returns>The Amazon SNS topic address.</returns>
    public Uri GetSendAddress(Uri hostAddress)
    {
        return GetEndpointAddress(hostAddress);
    }

    IEnumerable<string> GetSettingStrings()
    {
        if (Durable)
            yield return "durable";

        if (AutoDelete)
            yield return "auto-delete";
    }

    /// <summary>Formats the configured topic lifetime for diagnostics.</summary>
    /// <returns>The diagnostic settings description.</returns>
    public override string ToString()
    {
        return string.Join(", ", GetSettingStrings());
    }
}
