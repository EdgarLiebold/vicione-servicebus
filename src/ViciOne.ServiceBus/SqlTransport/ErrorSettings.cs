using ViciOne.ServiceBus.SqlTransport.Topology;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>Defines settings for error.</summary>
public interface ErrorSettings :
    EntitySettings
{
    /// <summary>Creates the topic, queue, and subscription topology required by this error destination.</summary>
    /// <returns>The broker topology.</returns>
    BrokerTopology GetBrokerTopology();
}
