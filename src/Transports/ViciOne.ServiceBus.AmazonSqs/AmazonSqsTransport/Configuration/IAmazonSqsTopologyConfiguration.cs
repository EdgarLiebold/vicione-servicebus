using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.AmazonSqs.Configuration;

/// <summary>Exposes Amazon SQS send, Amazon SNS publish, and subscription topology configuration.</summary>
public interface IAmazonSqsTopologyConfiguration :
    ITopologyConfiguration
{
    /// <summary>Gets the Amazon SNS publish-topology configurator.</summary>
    new IAmazonSqsPublishTopologyConfigurator Publish { get; }

    /// <summary>Gets the Amazon SQS send-topology configurator.</summary>
    new IAmazonSqsSendTopologyConfigurator Send { get; }

    /// <summary>Gets the Amazon SNS-to-SQS consume-topology configurator.</summary>
    new IAmazonSqsConsumeTopologyConfigurator Consume { get; }
}
