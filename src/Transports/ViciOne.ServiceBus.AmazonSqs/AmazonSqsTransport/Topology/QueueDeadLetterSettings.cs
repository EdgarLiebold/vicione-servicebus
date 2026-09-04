using System.Linq;
using ViciOne.ServiceBus.AmazonSqs.Configuration;

namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>
/// Provides a queue dead letter settings implementation.
/// </summary>
public class QueueDeadLetterSettings :
    AmazonSqsQueueSubscriptionConfigurator,
    DeadLetterSettings
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="source">The source value.</param>
    /// <param name="queueName">The queue name value.</param>
    public QueueDeadLetterSettings(ReceiveSettings source, string queueName)
        : base(queueName, source.Durable, source.AutoDelete)
    {
        QueueTags = source.Tags.ToDictionary(x => x.Key, x => x.Value);
        QueueAttributes = source.QueueAttributes.ToDictionary(x => x.Key, x => x.Value);
    }

    /// <summary>
    /// Gets broker topology.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public BrokerTopology GetBrokerTopology()
    {
        var builder = new PublishEndpointBrokerTopologyBuilder();

        builder.CreateQueue(EntityName, Durable, AutoDelete, QueueAttributes, QueueSubscriptionAttributes, QueueTags);

        return builder.BuildBrokerTopology();
    }
}
