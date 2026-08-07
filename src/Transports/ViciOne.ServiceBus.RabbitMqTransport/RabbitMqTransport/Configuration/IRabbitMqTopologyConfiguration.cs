// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.RabbitMqTransport.Configuration
{
    using ViciOne.ServiceBus.Configuration;


    public interface IRabbitMqTopologyConfiguration :
        ITopologyConfiguration
    {
        new IRabbitMqPublishTopologyConfigurator Publish { get; }

        new IRabbitMqSendTopologyConfigurator Send { get; }

        new IRabbitMqConsumeTopologyConfigurator Consume { get; }
    }
}
