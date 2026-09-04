using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>
/// Defines the contract for rabbit mq topology configuration.
/// </summary>
public interface IRabbitMqTopologyConfiguration :
    ITopologyConfiguration
{
    /// <summary>
    /// Gets the publish value.
    /// </summary>
    new IRabbitMqPublishTopologyConfigurator Publish { get; }

    /// <summary>
    /// Gets the send value.
    /// </summary>
    new IRabbitMqSendTopologyConfigurator Send { get; }

    /// <summary>
    /// Gets the consume value.
    /// </summary>
    new IRabbitMqConsumeTopologyConfigurator Consume { get; }
}
