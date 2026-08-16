namespace ViciOne.ServiceBus.AmazonSqsTransport.Configuration;

using ViciOne.ServiceBus.Configuration;


public interface IAmazonSqsTopologyConfiguration :
    ITopologyConfiguration
{
    new IAmazonSqsPublishTopologyConfigurator Publish { get; }

    new IAmazonSqsSendTopologyConfigurator Send { get; }

    new IAmazonSqsConsumeTopologyConfigurator Consume { get; }
}
