using System;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DependencyInjection;

namespace ViciOne.ServiceBus.EventHubs.Configuration;

/// <summary>
/// Provides an event hub registration rider factory implementation.
/// </summary>
public class EventHubRegistrationRiderFactory :
    IRegistrationRiderFactory<IEventHubRider>
{
    readonly Action<IRiderRegistrationContext, IEventHubFactoryConfigurator> _configure;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    public EventHubRegistrationRiderFactory(Action<IRiderRegistrationContext, IEventHubFactoryConfigurator> configure)
    {
        _configure = configure;
    }

    /// <summary>
    /// Creates rider.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public IBusInstanceSpecification CreateRider(IRiderRegistrationContext context)
    {
        var configurator = new EventHubFactoryConfigurator();

        _configure?.Invoke(context, configurator);

        return configurator.Build(context);
    }
}
