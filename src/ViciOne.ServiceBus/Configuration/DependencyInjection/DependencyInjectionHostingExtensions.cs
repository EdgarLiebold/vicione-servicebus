using System;
using Microsoft.Extensions.Hosting;

#nullable enable
namespace ViciOne.ServiceBus;

public static class DependencyInjectionHostingExtensions
{
    /// <summary>
    /// Adds ViciOne.ServiceBus and its dependencies and allows consumers, sagas, and activities to be configured
    /// </summary>
    /// <param name="hostBuilder"></param>
    /// <param name="configure"></param>
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
    /// <param name="hostBuilder"></param>
    /// <param name="configure"></param>
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
    /// <param name="hostBuilder"></param>
    /// <param name="configure"></param>
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

    /// <summary>
    /// Adds the ViciOne.ServiceBus Mediator to the host, and allows consumers, sagas, and activities (which are not supported
    /// by the Mediator) to be configured.
    /// </summary>
    /// <param name="hostBuilder"></param>
    /// <param name="configure"></param>
    public static IHostBuilder UseMediator(this IHostBuilder hostBuilder, Action<HostBuilderContext, IMediatorRegistrationConfigurator>? configure = null)
    {
        hostBuilder.ConfigureServices((hostContext, services) =>
        {
            services.AddMediator(configurator =>
            {
                configure?.Invoke(hostContext, configurator);
            });
        });

        return hostBuilder;
    }
}
