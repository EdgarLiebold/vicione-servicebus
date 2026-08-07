// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.AzureServiceBusTransport.Configuration
{
    using ViciOne.ServiceBus.Configuration;


    public interface IServiceBusTopologyConfiguration :
        ITopologyConfiguration
    {
        new IServiceBusPublishTopologyConfigurator Publish { get; }

        new IServiceBusSendTopologyConfigurator Send { get; }

        new IServiceBusConsumeTopologyConfigurator Consume { get; }
    }
}
