using ViciOne.ServiceBus.ActiveMq.Configuration;

namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>Defines the ActiveMQ queue and topology used for skipped messages.</summary>
public class ActiveMqDeadLetterSettings :
    ActiveMqQueueBindingConfigurator,
    DeadLetterSettings
{
    /// <summary>Creates dead-letter queue settings that inherit source lifecycle behavior.</summary>
    /// <param name="source">The source endpoint settings.</param>
    /// <param name="queueName">The dead-letter queue name.</param>
    public ActiveMqDeadLetterSettings(EntitySettings source, string queueName)
        : base(queueName, source.Durable, source.AutoDelete)
    {
    }

    /// <summary>Builds topology containing the dead-letter queue.</summary>
    /// <returns>The queue-only broker topology.</returns>
    public BrokerTopology GetBrokerTopology()
    {
        var builder = new PublishEndpointBrokerTopologyBuilder();

        builder.CreateQueue(EntityName, Durable, AutoDelete);

        return builder.BuildBrokerTopology();
    }
}
