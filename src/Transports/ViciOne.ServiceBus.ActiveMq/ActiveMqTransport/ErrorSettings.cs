using ViciOne.ServiceBus.ActiveMq.Topology;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Defines the ActiveMQ destination and topology used for faulted messages.</summary>
public interface ErrorSettings :
    EntitySettings
{
    /// <summary>Creates the topic and queue topology required by this error destination.</summary>
    /// <returns>The broker topology.</returns>
    BrokerTopology GetBrokerTopology();
}
