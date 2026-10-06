using System.Collections.Generic;
using ViciOne.ServiceBus.AmazonSqs.Configuration;

namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>Represents Amazon SQS queue settings and topology for a send destination.</summary>
public class QueueSendSettings :
    AmazonSqsQueueConfigurator,
    SendSettings
{
    /// <summary>Initializes queue send settings from an endpoint address.</summary>
    /// <param name="address">The Amazon SQS queue endpoint address.</param>
    public QueueSendSettings(AmazonSqsEndpointAddress address)
        : base(address.Name, address.Durable, address.AutoDelete)
    {
    }

    /// <summary>Builds topology containing the destination queue.</summary>
    /// <returns>The queue broker topology.</returns>
    public BrokerTopology GetBrokerTopology()
    {
        var builder = new SendEndpointBrokerTopologyBuilder();

        builder.Queue = builder.CreateQueue(EntityName, Durable, AutoDelete, QueueAttributes, QueueSubscriptionAttributes, Tags);

        return builder.BuildBrokerTopology();
    }

    IEnumerable<string> GetSettingStrings()
    {
        if (Durable)
            yield return "durable";

        if (AutoDelete)
            yield return "auto-delete";
    }

    /// <summary>Formats the configured queue lifetime for diagnostics.</summary>
    /// <returns>The diagnostic settings description.</returns>
    public override string ToString()
    {
        return string.Join(", ", GetSettingStrings());
    }
}
