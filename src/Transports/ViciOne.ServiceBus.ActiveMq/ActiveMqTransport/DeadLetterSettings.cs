using ViciOne.ServiceBus.ActiveMq.Topology;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Defines the ActiveMQ destination and topology used for skipped messages.</summary>
public interface DeadLetterSettings :
    EntitySettings
{
    /// <summary>Creates the topic and queue topology required by this dead-letter destination.</summary>
    /// <returns>The broker topology.</returns>
    BrokerTopology GetBrokerTopology();
}
