// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    using System;


    public class TimeoutHandlerConfigurationObserver :
        IHandlerConfigurationObserver
    {
        readonly Action<ITimeoutConfigurator> _configure;

        public TimeoutHandlerConfigurationObserver(Action<ITimeoutConfigurator> configure)
        {
            _configure = configure;
        }

        void IHandlerConfigurationObserver.HandlerConfigured<T>(IHandlerConfigurator<T> configurator)
        {
            var specification = new TimeoutSpecification<T>();

            _configure?.Invoke(specification);

            configurator.AddPipeSpecification(specification);
        }
    }
}
