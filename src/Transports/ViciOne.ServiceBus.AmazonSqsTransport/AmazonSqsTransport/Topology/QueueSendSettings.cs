using System.Collections.Generic;
using ViciOne.ServiceBus.AmazonSqsTransport.Configuration;

namespace ViciOne.ServiceBus.AmazonSqsTransport.Topology;

public class QueueSendSettings :
    AmazonSqsQueueConfigurator,
    SendSettings
{
    public QueueSendSettings(AmazonSqsEndpointAddress address)
        : base(address.Name, address.Durable, address.AutoDelete)
    {
    }

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

    public override string ToString()
    {
        return string.Join(", ", GetSettingStrings());
    }
}
