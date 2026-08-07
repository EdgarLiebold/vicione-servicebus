// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    public interface IMessageConfigurationObserver
    {
        /// <summary>
        /// Called when a message pipeline is configured, for the very first time
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="configurator"></param>
        void MessageConfigured<TMessage>(IConsumePipeConfigurator configurator)
            where TMessage : class;
    }
}
