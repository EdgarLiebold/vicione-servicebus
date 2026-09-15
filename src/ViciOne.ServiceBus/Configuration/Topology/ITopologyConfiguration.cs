namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides the message, send, publish, and consume topology owned by one bus configuration.</summary>
public interface ITopologyConfiguration :
    ISpecification
{
    /// <summary>Gets the message-contract topology configurator.</summary>
    IMessageTopologyConfigurator Message { get; }

    /// <summary>Gets the send topology configurator.</summary>
    ISendTopologyConfigurator Send { get; }

    /// <summary>Gets the publish topology configurator.</summary>
    IPublishTopologyConfigurator Publish { get; }

    /// <summary>Gets the consume topology configurator.</summary>
    IConsumeTopologyConfigurator Consume { get; }
}
