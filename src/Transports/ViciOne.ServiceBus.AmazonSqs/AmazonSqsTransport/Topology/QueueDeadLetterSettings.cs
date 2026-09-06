using System.Linq;
using ViciOne.ServiceBus.AmazonSqs.Configuration;

namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>Derives skipped-message queue settings from a receive queue.</summary>
public class QueueDeadLetterSettings :
    AmazonSqsQueueSubscriptionConfigurator,
    DeadLetterSettings
{
    /// <summary>Initializes skipped-message queue settings with cloned source tags and attributes.</summary>
    /// <param name="source">The source receive-queue settings.</param>
    /// <param name="queueName">The skipped-message queue name.</param>
    public QueueDeadLetterSettings(ReceiveSettings source, string queueName)
        : base(queueName, source.Durable, source.AutoDelete)
    {
        QueueTags = source.Tags.ToDictionary(x => x.Key, x => x.Value);
        QueueAttributes = source.QueueAttributes.ToDictionary(x => x.Key, x => x.Value);
    }

    /// <summary>Builds topology containing the skipped-message queue.</summary>
    /// <returns>The queue broker topology.</returns>
    public BrokerTopology GetBrokerTopology()
    {
        var builder = new PublishEndpointBrokerTopologyBuilder();

        builder.CreateQueue(EntityName, Durable, AutoDelete, QueueAttributes, QueueSubscriptionAttributes, QueueTags);

        return builder.BuildBrokerTopology();
    }
}
