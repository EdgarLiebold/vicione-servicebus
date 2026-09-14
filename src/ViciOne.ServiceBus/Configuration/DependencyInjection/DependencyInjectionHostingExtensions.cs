using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Registers service-bus instances with the .NET Generic Host.</summary>
public static class DependencyInjectionHostingExtensions
{
    /// <summary>Registers the default bus and its consumers, sagas, activities, and transport.</summary>
    /// <param name="hostBuilder">The host builder whose services receive the bus registration.</param>
    /// <param name="configure">An optional callback that configures the bus for the current host context.</param>
    /// <returns>The same host builder.</returns>
    public static IHostBuilder UseViciOneServiceBus(this IHostBuilder hostBuilder, Action<HostBuilderContext, IBusRegistrationConfigurator>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(hostBuilder);

        hostBuilder.ConfigureServices((hostContext, services) =>
        {
            services.AddViciOneServiceBus(configurator =>
            {
                configure?.Invoke(hostContext, configurator);
            });
        });

        return hostBuilder;
    }

    /// <summary>Registers a typed bus whose application contract is <typeparamref name="TBus" />.</summary>
    /// <typeparam name="TBus">The application-facing bus contract.</typeparam>
    /// <param name="hostBuilder">The host builder whose services receive the bus registration.</param>
    /// <param name="configure">An optional callback that configures the typed bus for the current host context.</param>
    /// <returns>The same host builder.</returns>
    public static IHostBuilder UseViciOneServiceBus<TBus>(this IHostBuilder hostBuilder,
        Action<HostBuilderContext, IBusRegistrationConfigurator<TBus>>? configure = null)
        where TBus : class, IBus
    {
        ArgumentNullException.ThrowIfNull(hostBuilder);

        hostBuilder.ConfigureServices((hostContext, services) =>
        {
            services.AddViciOneServiceBus<TBus>(configurator =>
            {
                configure?.Invoke(hostContext, configurator);
            });
        });

        return hostBuilder;
    }

    /// <summary>Registers a typed bus through an explicit bus-instance implementation.</summary>
    /// <typeparam name="TBus">The application-facing bus contract.</typeparam>
    /// <typeparam name="TBusInstance">The implementation that binds the contract to its runtime bus.</typeparam>
    /// <param name="hostBuilder">The host builder whose services receive the bus registration.</param>
    /// <param name="configure">An optional callback that configures the typed bus for the current host context.</param>
    /// <returns>The same host builder.</returns>
    public static IHostBuilder UseViciOneServiceBus<TBus, TBusInstance>(this IHostBuilder hostBuilder,
        Action<HostBuilderContext, IBusRegistrationConfigurator<TBus>>? configure = null)
        where TBus : class, IBus
        where TBusInstance : BusInstance<TBus>, TBus
    {
        ArgumentNullException.ThrowIfNull(hostBuilder);

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
