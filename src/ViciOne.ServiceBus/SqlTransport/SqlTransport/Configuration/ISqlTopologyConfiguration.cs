// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.SqlTransport.Configuration
{
    using ViciOne.ServiceBus.Configuration;


    public interface ISqlTopologyConfiguration :
        ITopologyConfiguration
    {
        new ISqlPublishTopologyConfigurator Publish { get; }

        new ISqlSendTopologyConfigurator Send { get; }

        new ISqlConsumeTopologyConfigurator Consume { get; }
    }
}
