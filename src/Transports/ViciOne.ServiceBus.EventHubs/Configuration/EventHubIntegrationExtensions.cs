using System;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.EventHubs.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Adds an Event Hubs rider to a bus registration.</summary>
public static class EventHubIntegrationExtensions
{
    /// <summary>Registers and configures the Event Hubs rider for the default bus.</summary>
    /// <param name="configurator">The rider registration configurator.</param>
    /// <param name="configure">Configures Event Hubs hosts, producers, and receive endpoints.</param>
    public static void UsingEventHub(this IRiderRegistrationConfigurator configurator,
        Action<IRiderRegistrationContext, IEventHubFactoryConfigurator> configure)
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));

        var factory = new EventHubRegistrationRiderFactory(configure);
        configurator.SetRiderFactory(factory);

        configurator.TryAddScoped<IEventHubRider, IEventHubProducerProvider>(GetCurrentProducerProvider);
    }

    /// <summary>Registers and configures the Event Hubs rider for a specific bus instance.</summary>
    /// <typeparam name="TBus">The bus type to which the rider is bound.</typeparam>
    /// <param name="configurator">The rider registration configurator.</param>
    /// <param name="configure">Configures Event Hubs hosts, producers, and receive endpoints.</param>
    public static void UsingEventHub<TBus>(this IRiderRegistrationConfigurator<TBus> configurator,
        Action<IRiderRegistrationContext, IEventHubFactoryConfigurator> configure)
        where TBus : class, IBus
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));

        var factory = new EventHubRegistrationRiderFactory(configure);
        configurator.SetRiderFactory(factory);

        configurator.TryAddScoped<IEventHubRider, Bind<TBus, IEventHubProducerProvider>>((rider, provider) =>
            Bind<TBus>.Create(GetCurrentProducerProvider(rider, provider)));
    }

    static IEventHubProducerProvider GetCurrentProducerProvider(IEventHubRider rider, IServiceProvider provider)
    {
        var contextProvider = provider.GetService<IScopedConsumeContextProvider>();
        if (contextProvider != null)
        {
            return contextProvider.HasContext
                ? rider.GetProducerProvider(contextProvider.GetContext())
                : rider.GetProducerProvider();
        }

        return rider.GetProducerProvider(provider.GetService<ConsumeContext>());
    }
}
