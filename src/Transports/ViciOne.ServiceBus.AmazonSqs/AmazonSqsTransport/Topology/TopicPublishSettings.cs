using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.AmazonSqs.Configuration;

namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>
/// Provides a topic publish settings implementation.
/// </summary>
public class TopicPublishSettings :
    AmazonSqsTopicConfigurator,
    PublishSettings
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="address">The address value.</param>
    public TopicPublishSettings(AmazonSqsEndpointAddress address)
        : base(address.Name, address.Durable, address.AutoDelete)
    {
    }

    /// <summary>
    /// Gets send address.
    /// </summary>
    /// <param name="hostAddress">The host address value.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Returns the string representation of this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override string ToString()
    {
        return string.Join(", ", GetSettingStrings());
    }
}
