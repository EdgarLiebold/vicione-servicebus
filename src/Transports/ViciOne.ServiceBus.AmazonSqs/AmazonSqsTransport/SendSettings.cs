using ViciOne.ServiceBus.AmazonSqs.Topology;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Defines Amazon SQS entity and topology settings for a send destination.</summary>
public interface SendSettings :
    EntitySettings
{
    /// <summary>Creates the topic, queue, and subscription topology required by this send destination.</summary>
    /// <returns>The broker topology.</returns>
    BrokerTopology GetBrokerTopology();
}
