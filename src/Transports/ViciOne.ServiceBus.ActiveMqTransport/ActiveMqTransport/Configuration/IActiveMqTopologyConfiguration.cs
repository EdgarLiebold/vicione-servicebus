// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.ActiveMqTransport.Configuration
{
    using ViciOne.ServiceBus.Configuration;


    public interface IActiveMqTopologyConfiguration :
        ITopologyConfiguration
    {
        new IActiveMqPublishTopologyConfigurator Publish { get; }

        new IActiveMqSendTopologyConfigurator Send { get; }

        new IActiveMqConsumeTopologyConfigurator Consume { get; }
    }
}
