using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.InMemoryTransport;
using ViciOne.ServiceBus.InMemoryTransport.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus;

public static class InMemoryConfigurationExtensions
{
    /// <summary>
    /// Configure and create an in-memory bus
    /// </summary>
    /// <param name="selector">Hang off the selector interface for visibility</param>
    /// <param name="configure">The configuration callback to configure the bus</param>
    /// <returns></returns>
    public static IBusControl CreateUsingInMemory(this IBusFactorySelector selector, Action<IInMemoryBusFactoryConfigurator> configure)
    {
        return InMemoryBus.Create(configure);
    }

    /// <summary>
    /// Configure and create an in-memory bus
    /// </summary>
    /// <param name="selector">Hang off the selector interface for visibility</param>
    /// <param name="baseAddress">Override the default base address</param>
    /// <param name="configure">The configuration callback to configure the bus</param>
    /// <returns></returns>
    public static IBusControl CreateUsingInMemory(this IBusFactorySelector selector, Uri baseAddress, Action<IInMemoryBusFactoryConfigurator> configure)
    {
        return InMemoryBus.Create(baseAddress, configure);
    }

    /// <summary>
    /// Configure ViciOne.ServiceBus to use the In-Memory transport.
    /// </summary>
    /// <param name="configurator">The registration configurator (configured via AddViciOneServiceBus)</param>
    /// <param name="configure">The configuration callback for the bus factory</param>
    public static void UsingInMemory(this IBusRegistrationConfigurator configurator,
        Action<IBusRegistrationContext, IInMemoryBusFactoryConfigurator> configure = null)
    {
        UsingInMemory(configurator, null, configure);
    }

    /// <summary>
    /// Configure ViciOne.ServiceBus to use the In-Memory transport.
    /// </summary>
    /// <param name="configurator">The registration configurator (configured via AddViciOneServiceBus)</param>
    /// <param name="baseAddress">The base Address of the transport</param>
    /// <param name="configure">The configuration callback for the bus factory</param>
    public static void UsingInMemory(this IBusRegistrationConfigurator configurator, Uri baseAddress,
        Action<IBusRegistrationContext, IInMemoryBusFactoryConfigurator> configure = null)
    {
        configurator.SetBusFactory(new InMemoryRegistrationBusFactory(baseAddress, configure));
        configurator.TryAddSingleton<IDurableSendDispatcher<IBus>, InMemoryDurableSendDispatcher<IBus>>();

        configurator.TryAddSingleton(provider =>
        {
            var delayProvider = provider.GetRequiredService<Bind<IBus, IBusInstance>>().Value as IInMemoryDelayProvider;

            return delayProvider ?? throw new ConfigurationException("The default bus instance is not an InMemory Bus Instance");
        });
    }

    /// <summary>
    /// Configure ViciOne.ServiceBus to use the In-Memory transport for the multi-bus instance.
    /// </summary>
    /// <param name="configurator">The typed registration configurator.</param>
    /// <param name="configure">The configuration callback for the bus factory.</param>
    public static void UsingInMemory<TBus>(this IBusRegistrationConfigurator<TBus> configurator,
        Action<IBusRegistrationContext, IInMemoryBusFactoryConfigurator> configure = null)
        where TBus : class, IBus
    {
        UsingInMemory(configurator, null, configure);
    }

    /// <summary>
    /// Configure ViciOne.ServiceBus to use the In-Memory transport for the multi-bus instance.
    /// </summary>
    /// <param name="configurator">The registration configurator (configured via AddViciOneServiceBus)</param>
    /// <param name="baseAddress">The base Address of the transport</param>
    /// <param name="configure">The configuration callback for the bus factory</param>
    public static void UsingInMemory<TBus>(this IBusRegistrationConfigurator<TBus> configurator, Uri baseAddress,
        Action<IBusRegistrationContext, IInMemoryBusFactoryConfigurator> configure = null)
        where TBus : class, IBus
    {
        configurator.SetBusFactory(new InMemoryRegistrationBusFactory(baseAddress, configure));
        configurator.TryAddSingleton<IDurableSendDispatcher<TBus>, InMemoryDurableSendDispatcher<TBus>>();

        AddDelayProvider<TBus>(configurator);
    }

    static void AddDelayProvider<TBus>(IServiceCollection services)
        where TBus : class, IBus
    {
        services.TryAddSingleton(provider =>
        {
            var delayProvider = provider.GetRequiredService<IBusInstance<TBus>>().BusInstance as IInMemoryDelayProvider;

            return delayProvider ?? throw new ConfigurationException("The bus instance is not an InMemory Bus Instance");
        });

        services.TryAddSingleton(provider =>
        {
            var delayProvider = provider.GetRequiredService<IBusInstance<TBus>>().BusInstance as IInMemoryDelayProvider;

            return Bind<TBus>.Create(delayProvider
                ?? throw new ConfigurationException("The bus instance is not an InMemory Bus Instance"));
        });
    }
}
