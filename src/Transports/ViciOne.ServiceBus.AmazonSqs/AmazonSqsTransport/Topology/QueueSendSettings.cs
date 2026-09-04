using System.Collections.Generic;
using ViciOne.ServiceBus.AmazonSqs.Configuration;

namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>
/// Provides a queue send settings implementation.
/// </summary>
public class QueueSendSettings :
    AmazonSqsQueueConfigurator,
    SendSettings
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="address">The address value.</param>
    public QueueSendSettings(AmazonSqsEndpointAddress address)
        : base(address.Name, address.Durable, address.AutoDelete)
    {
    }

    /// <summary>
    /// Gets broker topology.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public BrokerTopology GetBrokerTopology()
    {
        var builder = new SendEndpointBrokerTopologyBuilder();

        builder.Queue = builder.CreateQueue(EntityName, Durable, AutoDelete);

        return builder.BuildBrokerTopology();
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
