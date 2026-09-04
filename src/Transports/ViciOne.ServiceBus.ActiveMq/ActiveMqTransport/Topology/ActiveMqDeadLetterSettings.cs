using ViciOne.ServiceBus.ActiveMq.Configuration;

namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>
/// Provides an active mq dead letter settings implementation.
/// </summary>
public class ActiveMqDeadLetterSettings :
    ActiveMqQueueBindingConfigurator,
    DeadLetterSettings
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="source">The source value.</param>
    /// <param name="queueName">The queue name value.</param>
    public ActiveMqDeadLetterSettings(EntitySettings source, string queueName)
        : base(queueName, source.Durable, source.AutoDelete)
    {
    }

    /// <summary>
    /// Gets broker topology.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public BrokerTopology GetBrokerTopology()
    {
        var builder = new PublishEndpointBrokerTopologyBuilder();

        builder.CreateQueue(EntityName, Durable, AutoDelete);

        return builder.BuildBrokerTopology();
    }
}
