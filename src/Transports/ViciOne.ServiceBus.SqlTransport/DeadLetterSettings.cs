using ViciOne.ServiceBus.SqlTransport.Topology;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>Defines settings for dead letter.</summary>
public interface DeadLetterSettings :
    EntitySettings
{
    /// <summary>Creates the topic, queue, and subscription topology required by this dead-letter destination.</summary>
    /// <returns>The broker topology.</returns>
    BrokerTopology GetBrokerTopology();
}
