using System;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.EventHubs.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides extension methods for event hub integration.
/// </summary>
public static class EventHubIntegrationExtensions
{
    /// <summary>
    /// Performs the using event hub operation.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="configure">The configuration callback.</param>
    public static void UsingEventHub(this IRiderRegistrationConfigurator configurator,
        Action<IRiderRegistrationContext, IEventHubFactoryConfigurator> configure)
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));

        var factory = new EventHubRegistrationRiderFactory(configure);
        configurator.SetRiderFactory(factory);

        configurator.TryAddScoped<IEventHubRider, IEventHubProducerProvider>(GetCurrentProducerProvider);
    }

    /// <summary>
    /// Performs the using event hub operation.
    /// </summary>
    /// <typeparam name="TBus">The t bus type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="configure">The configuration callback.</param>
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
