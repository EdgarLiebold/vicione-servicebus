using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.AmazonSqsTransport.Configuration;

public interface IAmazonSqsTopologyConfiguration :
    ITopologyConfiguration
{
    new IAmazonSqsPublishTopologyConfigurator Publish { get; }

    new IAmazonSqsSendTopologyConfigurator Send { get; }

    new IAmazonSqsConsumeTopologyConfigurator Consume { get; }
}
