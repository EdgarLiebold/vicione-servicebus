using System;
using System.Linq;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Mediator;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Provides dependency-injection registration for the mediator capability package.</summary>
public static class MediatorServiceCollectionExtensions
{
    /// <summary>Adds the mediator with the specified base address.</summary>
    /// <param name="services">The service collection that receives mediator services.</param>
    /// <param name="baseAddress">The loopback address used as the root for mediator endpoints.</param>
    /// <param name="configure">The callback that declares message limits, handlers, and pipelines.</param>
    /// <returns>The same service collection for continued registration.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services" />, <paramref name="baseAddress" />, or <paramref name="configure" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="baseAddress" /> is not an absolute loopback base address.</exception>
    /// <exception cref="ConfigurationException">The service collection already contains a mediator registration or the mediator configuration is invalid.</exception>
    public static IServiceCollection AddMediator(this IServiceCollection services, Uri baseAddress,
        Action<IMediatorRegistrationConfigurator> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(baseAddress);
        ArgumentNullException.ThrowIfNull(configure);
        return AddMediatorCore(services, MediatorFactory.ValidateBaseAddress(baseAddress), configure);
    }

    /// <summary>Adds the mediator using its default loopback base address.</summary>
    /// <param name="services">The service collection that receives mediator services.</param>
    /// <param name="configure">The callback that declares message limits, handlers, and pipelines.</param>
    /// <returns>The same service collection for continued registration.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services" /> or <paramref name="configure" /> is <see langword="null" />.</exception>
    /// <exception cref="ConfigurationException">The service collection already contains a mediator registration or the mediator configuration is invalid.</exception>
    public static IServiceCollection AddMediator(this IServiceCollection services,
        Action<IMediatorRegistrationConfigurator> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);
        return AddMediatorCore(services, MediatorFactory.ValidateBaseAddress(null), configure);
    }

    static IServiceCollection AddMediatorCore(
        IServiceCollection services,
        Uri baseAddress,
        Action<IMediatorRegistrationConfigurator> configure)
    {
        if (services.Any(descriptor => descriptor.ServiceType == typeof(IMediator)))
        {
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Mediator", "default",
                    "AddMediator() was already called and may only be called once per container",
                    "Remove the duplicate mediator registration"));
        }

        var configurator = new ServiceCollectionMediatorConfigurator(services, baseAddress);
        configure(configurator);
        services.AddMetrics();
        configurator.Complete();
        return services;
    }
}
