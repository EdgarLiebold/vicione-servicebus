// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System.ComponentModel;


    public interface IHandlerConfigurationObserver
    {
        /// <summary>
        /// Called when a consumer/message combination is configured
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="configurator"></param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        void HandlerConfigured<TMessage>(IHandlerConfigurator<TMessage> configurator)
            where TMessage : class;
    }
}
