using ViciOne.ServiceBus.AzureServiceBus.Topology;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>The settings for sending to an endpoint.</summary>
public interface SendSettings
{
    /// <summary>The path of the messaging entity.</summary>
    string EntityPath { get; }

    /// <summary>Gets broker topology.</summary>
    /// <returns>The broker topology.</returns>
    BrokerTopology GetBrokerTopology();
}
