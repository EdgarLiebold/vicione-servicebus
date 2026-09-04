using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.ActiveMq.Configuration;

/// <summary>
/// Defines the contract for active mq topology configuration.
/// </summary>
public interface IActiveMqTopologyConfiguration :
    ITopologyConfiguration
{
    /// <summary>
    /// Gets the publish value.
    /// </summary>
    new IActiveMqPublishTopologyConfigurator Publish { get; }

    /// <summary>
    /// Gets the send value.
    /// </summary>
    new IActiveMqSendTopologyConfigurator Send { get; }

    /// <summary>
    /// Gets the consume value.
    /// </summary>
    new IActiveMqConsumeTopologyConfigurator Consume { get; }
}
