using System;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DependencyInjection;

namespace ViciOne.ServiceBus.EventHubIntegration.Configuration;

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
