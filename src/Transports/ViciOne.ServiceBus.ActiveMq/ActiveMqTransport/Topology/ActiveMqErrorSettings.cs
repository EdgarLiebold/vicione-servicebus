using ViciOne.ServiceBus.ActiveMq.Configuration;

namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>Defines the ActiveMQ queue and topology used for faulted messages.</summary>
public class ActiveMqErrorSettings :
    ActiveMqQueueBindingConfigurator,
    ErrorSettings
{
    /// <summary>Creates error-queue settings that inherit source lifecycle behavior.</summary>
    /// <param name="source">The source endpoint settings.</param>
    /// <param name="queueName">The error queue name.</param>
    public ActiveMqErrorSettings(EntitySettings source, string queueName)
        : base(queueName, source.Durable, source.AutoDelete)
    {
    }

    /// <summary>Builds topology containing the error queue.</summary>
    /// <returns>The queue-only broker topology.</returns>
    public BrokerTopology GetBrokerTopology()
    {
        var builder = new PublishEndpointBrokerTopologyBuilder();

        builder.CreateQueue(EntityName, Durable, AutoDelete);

        return builder.BuildBrokerTopology();
    }
}
