using ViciOne.ServiceBus.AmazonSqs.Topology;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Defines the contract for send settings.
/// </summary>
public interface SendSettings :
    EntitySettings
{
    /// <summary>
    /// Return the BrokerTopology to apply at startup (to create exchange and queue if binding is specified)
    /// </summary>
    /// <returns></returns>
    BrokerTopology GetBrokerTopology();
}
