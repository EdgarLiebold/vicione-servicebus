using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.ActiveMq.Configuration;

/// <summary>Exposes ActiveMQ publish, send, and consume topology configurators.</summary>
public interface IActiveMqTopologyConfiguration :
    ITopologyConfiguration
{
    /// <summary>Gets the publish-topology configurator.</summary>
    new IActiveMqPublishTopologyConfigurator Publish { get; }

    /// <summary>Gets the send-topology configurator.</summary>
    new IActiveMqSendTopologyConfigurator Send { get; }

    /// <summary>Gets the consume-topology configurator.</summary>
    new IActiveMqConsumeTopologyConfigurator Consume { get; }
}
