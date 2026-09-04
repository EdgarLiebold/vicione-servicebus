using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.SqlTransport.Configuration;

/// <summary>
/// Defines the contract for sql topology configuration.
/// </summary>
public interface ISqlTopologyConfiguration :
    ITopologyConfiguration
{
    /// <summary>
    /// Gets the publish value.
    /// </summary>
    new ISqlPublishTopologyConfigurator Publish { get; }

    /// <summary>
    /// Gets the send value.
    /// </summary>
    new ISqlSendTopologyConfigurator Send { get; }

    /// <summary>
    /// Gets the consume value.
    /// </summary>
    new ISqlConsumeTopologyConfigurator Consume { get; }
}
