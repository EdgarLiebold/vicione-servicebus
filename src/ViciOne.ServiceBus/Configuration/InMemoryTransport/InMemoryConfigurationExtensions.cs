using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.InMemoryTransport.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Exposes standalone and dependency-injection entry points for the in-memory transport.</summary>
public static class InMemoryConfigurationExtensions
{
    /// <summary>Creates a standalone in-memory bus at the default loopback address.</summary>
    /// <param name="selector">The transport-independent bus factory selector.</param>
    /// <param name="configure">The callback that configures the bus before construction.</param>
    /// <returns>The constructed bus control.</returns>
    public static IBusControl CreateUsingInMemory(this IBusFactorySelector selector, Action<IInMemoryBusFactoryConfigurator> configure)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(configure);
        return InMemoryBus.Create(configure);
    }

    /// <summary>Creates a standalone in-memory bus at a custom loopback address.</summary>
    /// <param name="selector">The transport-independent bus factory selector.</param>
    /// <param name="baseAddress">The base address assigned to the in-memory host.</param>
    /// <param name="configure">The callback that configures the bus before construction.</param>
    /// <returns>The constructed bus control.</returns>
    public static IBusControl CreateUsingInMemory(this IBusFactorySelector selector, Uri baseAddress, Action<IInMemoryBusFactoryConfigurator> configure)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(baseAddress);
        ArgumentNullException.ThrowIfNull(configure);
        return InMemoryBus.Create(baseAddress, configure);
    }

    /// <summary>Selects the in-memory transport for the default bus.</summary>
    /// <param name="configurator">The bus registration that will own the transport.</param>
    /// <param name="configure">An optional callback that configures the in-memory bus factory.</param>
    public static void UsingInMemory(this IBusRegistrationConfigurator configurator,
        Action<IBusRegistrationContext, IInMemoryBusFactoryConfigurator>? configure = null)
    {
        UsingInMemoryCore(configurator, null, configure);
    }

    /// <summary>Selects the in-memory transport at a custom address for the default bus.</summary>
    /// <param name="configurator">The bus registration that will own the transport.</param>
    /// <param name="baseAddress">The transport base address.</param>
    /// <param name="configure">An optional callback that configures the in-memory bus factory.</param>
    public static void UsingInMemory(this IBusRegistrationConfigurator configurator, Uri baseAddress,
        Action<IBusRegistrationContext, IInMemoryBusFactoryConfigurator>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(baseAddress);

        UsingInMemoryCore(configurator, baseAddress, configure);
    }

    /// <summary>Selects the in-memory transport for a typed bus.</summary>
    /// <typeparam name="TBus">The application-facing bus contract.</typeparam>
    /// <param name="configurator">The typed bus registration that will own the transport.</param>
    /// <param name="configure">An optional callback that configures the in-memory bus factory.</param>
    public static void UsingInMemory<TBus>(this IBusRegistrationConfigurator<TBus> configurator,
        Action<IBusRegistrationContext, IInMemoryBusFactoryConfigurator>? configure = null)
        where TBus : class, IBus
    {
        UsingInMemoryCore(configurator, null, configure);
    }

    /// <summary>Selects the in-memory transport at a custom address for a typed bus.</summary>
    /// <typeparam name="TBus">The application-facing bus contract.</typeparam>
    /// <param name="configurator">The typed bus registration that will own the transport.</param>
    /// <param name="baseAddress">The transport base address.</param>
    /// <param name="configure">An optional callback that configures the in-memory bus factory.</param>
    public static void UsingInMemory<TBus>(this IBusRegistrationConfigurator<TBus> configurator, Uri baseAddress,
        Action<IBusRegistrationContext, IInMemoryBusFactoryConfigurator>? configure = null)
        where TBus : class, IBus
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(baseAddress);

        UsingInMemoryCore(configurator, baseAddress, configure);
    }

    static void UsingInMemoryCore(IBusRegistrationConfigurator configurator, Uri? baseAddress,
        Action<IBusRegistrationContext, IInMemoryBusFactoryConfigurator>? configure)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        configurator.SetBusFactory(new InMemoryRegistrationBusFactory(baseAddress, configure));
        configurator.Services.TryAddSingleton<IDurableSendDispatcher<IBus>, InMemoryDurableSendDispatcher<IBus>>();

        configurator.Services.TryAddSingleton(provider =>
        {
            var delayProvider = provider.GetRequiredService<Bind<IBus, IBusInstance>>().Value as IInMemoryDelayProvider;

            return delayProvider ?? throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("In Memory", "unknown", "The default bus instance is not an InMemory Bus Instance", "Correct the named configuration before starting the host"));
        });
    }

    static void UsingInMemoryCore<TBus>(IBusRegistrationConfigurator<TBus> configurator, Uri? baseAddress,
        Action<IBusRegistrationContext, IInMemoryBusFactoryConfigurator>? configure)
        where TBus : class, IBus
    {
        ArgumentNullException.ThrowIfNull(configurator);

        configurator.SetBusFactory(new InMemoryRegistrationBusFactory(baseAddress, configure));
        configurator.Services.TryAddSingleton<IDurableSendDispatcher<TBus>, InMemoryDurableSendDispatcher<TBus>>();

        AddDelayProvider<TBus>(configurator.Services);
    }

    static void AddDelayProvider<TBus>(IServiceCollection services)
        where TBus : class, IBus
    {
        services.TryAddSingleton(provider =>
        {
            var delayProvider = provider.GetRequiredService<IBusInstance<TBus>>().BusInstance as IInMemoryDelayProvider;

            return delayProvider ?? throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("In Memory", "unknown", "The bus instance is not an InMemory Bus Instance", "Correct the named configuration before starting the host"));
        });

        services.TryAddSingleton(provider =>
        {
            var delayProvider = provider.GetRequiredService<IBusInstance<TBus>>().BusInstance as IInMemoryDelayProvider;

            return Bind<TBus>.Create(delayProvider
                ?? throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("In Memory", "unknown", "The bus instance is not an InMemory Bus Instance", "Correct the named configuration before starting the host")));
        });
    }
}
