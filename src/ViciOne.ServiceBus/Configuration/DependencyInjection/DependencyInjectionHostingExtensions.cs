using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides extension methods for dependency injection hosting.</summary>
public static class DependencyInjectionHostingExtensions
{
    /// <summary>Adds ViciOne.ServiceBus and its dependencies and allows consumers, sagas, and activities to be configured.</summary>
    /// <param name="hostBuilder">The host builder.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The configured vici one service bus.</returns>
    public static IHostBuilder UseViciOneServiceBus(this IHostBuilder hostBuilder, Action<HostBuilderContext, IBusRegistrationConfigurator>? configure = null)
    {
        hostBuilder.ConfigureServices((hostContext, services) =>
        {
            services.AddViciOneServiceBus(configurator =>
            {
                configure?.Invoke(hostContext, configurator);
            });
        });

        return hostBuilder;
    }

    /// <summary>
    /// Configure a ViciOne.ServiceBus MultiBus instance, using the specified <typeparamref name="TBus" /> bus type, which must inherit directly from <see cref="IBus" />.
    /// A dynamic type will be created to support the bus instance, which will be initialized when the <typeparamref name="TBus" /> type is retrieved
    /// from the container.
    /// </summary>
    /// <typeparam name="TBus">The bus type.</typeparam>
    /// <param name="hostBuilder">The host builder.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The configured vici one service bus.</returns>
    public static IHostBuilder UseViciOneServiceBus<TBus>(this IHostBuilder hostBuilder,
        Action<HostBuilderContext, IBusRegistrationConfigurator<TBus>>? configure = null)
        where TBus : class, IBus
    {
        hostBuilder.ConfigureServices((hostContext, services) =>
        {
            services.AddViciOneServiceBus<TBus>(configurator =>
            {
                configure?.Invoke(hostContext, configurator);
            });
        });

        return hostBuilder;
    }

    /// <summary>
    /// Configure a ViciOne.ServiceBus bus instance, using the specified <typeparamref name="TBus" /> bus type, which must inherit directly from <see cref="IBus" />.
    /// A type that implements <typeparamref name="TBus" /> is required, specified by the <typeparamref name="TBusInstance" /> parameter.
    /// </summary>
    /// <typeparam name="TBus">The bus type.</typeparam>
    /// <typeparam name="TBusInstance">The bus instance type.</typeparam>
    /// <param name="hostBuilder">The host builder.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The configured vici one service bus.</returns>
    public static IHostBuilder UseViciOneServiceBus<TBus, TBusInstance>(this IHostBuilder hostBuilder,
        Action<HostBuilderContext, IBusRegistrationConfigurator<TBus>>? configure = null)
        where TBus : class, IBus
        where TBusInstance : BusInstance<TBus>, TBus
    {
        hostBuilder.ConfigureServices((hostContext, services) =>
        {
            services.AddViciOneServiceBus<TBus, TBusInstance>(configurator =>
            {
                configure?.Invoke(hostContext, configurator);
            });
        });

        return hostBuilder;
    }

}
