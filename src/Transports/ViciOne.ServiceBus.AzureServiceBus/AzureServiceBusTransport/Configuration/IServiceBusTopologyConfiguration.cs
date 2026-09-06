using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>Groups the Azure Service Bus publish, send, and consume topology configurators.</summary>
public interface IServiceBusTopologyConfiguration :
    ITopologyConfiguration
{
    /// <summary>Gets publish-topology configuration.</summary>
    new IServiceBusPublishTopologyConfigurator Publish { get; }

    /// <summary>Gets send-topology configuration.</summary>
    new IServiceBusSendTopologyConfigurator Send { get; }

    /// <summary>Gets consume-topology configuration.</summary>
    new IServiceBusConsumeTopologyConfigurator Consume { get; }
}
