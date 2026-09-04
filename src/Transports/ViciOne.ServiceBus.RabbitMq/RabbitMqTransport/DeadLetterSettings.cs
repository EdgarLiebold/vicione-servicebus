using ViciOne.ServiceBus.RabbitMq.Topology;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Defines the contract for dead letter settings.
/// </summary>
public interface DeadLetterSettings :
    EntitySettings
{
    /// <summary>
    /// Return the BrokerTopology to apply at startup (to create exchange and queue if binding is specified)
    /// </summary>
    /// <returns></returns>
    BrokerTopology GetBrokerTopology();
}
