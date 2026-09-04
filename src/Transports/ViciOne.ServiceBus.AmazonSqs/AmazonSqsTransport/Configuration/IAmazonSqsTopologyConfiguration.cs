using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.AmazonSqs.Configuration;

/// <summary>
/// Defines the contract for amazon sqs topology configuration.
/// </summary>
public interface IAmazonSqsTopologyConfiguration :
    ITopologyConfiguration
{
    /// <summary>
    /// Gets the publish value.
    /// </summary>
    new IAmazonSqsPublishTopologyConfigurator Publish { get; }

    /// <summary>
    /// Gets the send value.
    /// </summary>
    new IAmazonSqsSendTopologyConfigurator Send { get; }

    /// <summary>
    /// Gets the consume value.
    /// </summary>
    new IAmazonSqsConsumeTopologyConfigurator Consume { get; }
}
