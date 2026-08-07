// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    /// <summary>
    /// Observes the configuration of message-specific topology
    /// </summary>
    public interface IMessageTopologyConfigurationObserver
    {
        void MessageTopologyCreated<T>(IMessageTopologyConfigurator<T> configuration)
            where T : class;
    }
}
