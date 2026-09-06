namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines topology configuration.</summary>
public interface ITopologyConfiguration :
    ISpecification
{
    /// <summary>Gets the message.</summary>
    IMessageTopologyConfigurator Message { get; }
    /// <summary>Gets the send.</summary>
    ISendTopologyConfigurator Send { get; }
    /// <summary>Gets the publish.</summary>
    IPublishTopologyConfigurator Publish { get; }
    /// <summary>Gets the consume.</summary>
    IConsumeTopologyConfigurator Consume { get; }
}
