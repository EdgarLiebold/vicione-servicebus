using System;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DependencyInjection;

namespace ViciOne.ServiceBus.EventHubs.Configuration;

/// <summary>Creates the bus-instance specification for a configured Event Hubs rider registration.</summary>
public class EventHubRegistrationRiderFactory :
    IRegistrationRiderFactory<IEventHubRider>
{
    readonly Action<IRiderRegistrationContext, IEventHubFactoryConfigurator> _configure;

    /// <summary>Stores the callback used to configure each rider instance.</summary>
    /// <param name="configure">Configures the Event Hubs rider from its registration context.</param>
    public EventHubRegistrationRiderFactory(Action<IRiderRegistrationContext, IEventHubFactoryConfigurator> configure)
    {
        _configure = configure;
    }

    /// <summary>Creates a configured Event Hubs bus-instance specification.</summary>
    /// <param name="context">The rider registration context.</param>
    /// <returns>The specification that will build and attach the rider.</returns>
    public IBusInstanceSpecification CreateRider(IRiderRegistrationContext context)
    {
        var configurator = new EventHubFactoryConfigurator();

        _configure?.Invoke(context, configurator);

        return configurator.Build(context);
    }
}
