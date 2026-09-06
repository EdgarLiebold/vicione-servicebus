using ViciOne.ServiceBus.AmazonSqs.Topology;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Defines the Amazon SQS entity and topology used for faulted-message delivery.</summary>
public interface ErrorSettings :
    EntitySettings
{
    /// <summary>Creates the topic, queue, and subscription topology required by this error destination.</summary>
    /// <returns>The broker topology.</returns>
    BrokerTopology GetBrokerTopology();
}
