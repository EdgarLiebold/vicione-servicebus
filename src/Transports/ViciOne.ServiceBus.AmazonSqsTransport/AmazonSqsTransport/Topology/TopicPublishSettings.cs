using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.AmazonSqsTransport.Configuration;

namespace ViciOne.ServiceBus.AmazonSqsTransport.Topology;

public class TopicPublishSettings :
    AmazonSqsTopicConfigurator,
    PublishSettings
{
    public TopicPublishSettings(AmazonSqsEndpointAddress address)
        : base(address.Name, address.Durable, address.AutoDelete)
    {
    }

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

    public override string ToString()
    {
        return string.Join(", ", GetSettingStrings());
    }
}
