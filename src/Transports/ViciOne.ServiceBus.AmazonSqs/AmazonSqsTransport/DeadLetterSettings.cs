using ViciOne.ServiceBus.AmazonSqs.Topology;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Defines the Amazon SQS entity and topology used for skipped-message delivery.</summary>
public interface DeadLetterSettings :
    EntitySettings
{
    /// <summary>Creates the topic, queue, and subscription topology required by this dead-letter destination.</summary>
    /// <returns>The broker topology.</returns>
    BrokerTopology GetBrokerTopology();
}
