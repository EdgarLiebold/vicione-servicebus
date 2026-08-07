// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    public interface IPublishTopologyConfigurationObserver
    {
        void MessageTopologyCreated<T>(IMessagePublishTopologyConfigurator<T> configurator)
            where T : class;
    }
}
