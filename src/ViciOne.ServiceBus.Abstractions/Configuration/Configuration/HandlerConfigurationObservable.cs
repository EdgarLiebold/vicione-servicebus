// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    using Util;


    public class HandlerConfigurationObservable :
        Connectable<IHandlerConfigurationObserver>,
        IHandlerConfigurationObserver
    {
        public void HandlerConfigured<TMessage>(IHandlerConfigurator<TMessage> configurator)
            where TMessage : class
        {
            ForEach(observer => observer.HandlerConfigured(configurator));
        }

        public void Method4()
        {
        }

        public void Method5()
        {
        }

        public void Method6()
        {
        }
    }
}
