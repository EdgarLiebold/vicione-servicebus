using ViciOne.ServiceBus.AzureServiceBusTransport.Topology;

namespace ViciOne.ServiceBus.AzureServiceBusTransport;

/// <summary>
/// The settings for sending to an endpoint
/// </summary>
public interface SendSettings
{
    /// <summary>
    /// The path of the messaging entity
    /// </summary>
    string EntityPath { get; }

    BrokerTopology GetBrokerTopology();
}
