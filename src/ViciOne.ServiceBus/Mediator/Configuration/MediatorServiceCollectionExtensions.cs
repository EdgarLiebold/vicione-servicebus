using System;
using System.Linq;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Mediator;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides dependency-injection registration for the mediator capability package.
/// </summary>
public static class MediatorServiceCollectionExtensions
{
    /// <summary>
    /// Adds the mediator with the specified base address.
    /// </summary>
    public static IServiceCollection AddMediator(this IServiceCollection services, Uri? baseAddress,
        Action<IMediatorRegistrationConfigurator>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        if (services.Any(descriptor => descriptor.ServiceType == typeof(IMediator)))
        {
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Mediator", "default",
                    "AddMediator() was already called and may only be called once per container",
                    "Remove the duplicate mediator registration"));
        }

        var configurator = new ServiceCollectionMediatorConfigurator(services, baseAddress);
        configure?.Invoke(configurator);
        services.AddMetrics();
        configurator.Complete();
        return services;
    }

    /// <summary>
    /// Adds the mediator using its default loopback base address.
    /// </summary>
    public static IServiceCollection AddMediator(this IServiceCollection services,
        Action<IMediatorRegistrationConfigurator>? configure = null) =>
        services.AddMediator(null, configure);
}
