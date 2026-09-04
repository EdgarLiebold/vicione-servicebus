namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for topology configuration.
/// </summary>
public interface ITopologyConfiguration :
    ISpecification
{
    /// <summary>
    /// Gets the message value.
    /// </summary>
    IMessageTopologyConfigurator Message { get; }
    /// <summary>
    /// Gets the send value.
    /// </summary>
    ISendTopologyConfigurator Send { get; }
    /// <summary>
    /// Gets the publish value.
    /// </summary>
    IPublishTopologyConfigurator Publish { get; }
    /// <summary>
    /// Gets the consume value.
    /// </summary>
    IConsumeTopologyConfigurator Consume { get; }
}
