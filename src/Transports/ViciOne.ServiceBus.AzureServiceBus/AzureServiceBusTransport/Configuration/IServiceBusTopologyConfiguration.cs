using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>
/// Defines the contract for service bus topology configuration.
/// </summary>
public interface IServiceBusTopologyConfiguration :
    ITopologyConfiguration
{
    /// <summary>
    /// Gets the publish value.
    /// </summary>
    new IServiceBusPublishTopologyConfigurator Publish { get; }

    /// <summary>
    /// Gets the send value.
    /// </summary>
    new IServiceBusSendTopologyConfigurator Send { get; }

    /// <summary>
    /// Gets the consume value.
    /// </summary>
    new IServiceBusConsumeTopologyConfigurator Consume { get; }
}
