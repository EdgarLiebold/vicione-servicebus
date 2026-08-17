namespace ViciOne.ServiceBus.EventHubIntegration.Configuration
{
    using System;
    using DependencyInjection;
    using ViciOne.ServiceBus.Configuration;
    using Microsoft.Extensions.DependencyInjection;


    public class EventHubRegistrationRiderFactory :
        IRegistrationRiderFactory<IEventHubRider>
    {
        readonly Action<IRiderRegistrationContext, IEventHubFactoryConfigurator> _configure;

        public EventHubRegistrationRiderFactory(Action<IRiderRegistrationContext, IEventHubFactoryConfigurator> configure)
        {
            _configure = configure;
        }

        public IBusInstanceSpecification CreateRider(IRiderRegistrationContext context)
        {
            var configurator = new EventHubFactoryConfigurator();

            _configure?.Invoke(context, configurator);

            return configurator.Build(context);
        }
    }
}
