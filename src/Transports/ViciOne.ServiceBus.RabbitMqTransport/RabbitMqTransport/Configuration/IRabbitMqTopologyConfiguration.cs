using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.RabbitMqTransport.Configuration;

public interface IRabbitMqTopologyConfiguration :
    ITopologyConfiguration
{
    new IRabbitMqPublishTopologyConfigurator Publish { get; }

    new IRabbitMqSendTopologyConfigurator Send { get; }

    new IRabbitMqConsumeTopologyConfigurator Consume { get; }
}
