using System;
using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures the .NET health-check registration associated with a bus.</summary>
public static class ViciOneServiceBusHealthCheckOptionsExtensions
{
    /// <summary>Configures the health-check name, failure-status floor, and tags for the default bus.</summary>
    /// <param name="configurator">The default bus registration to configure.</param>
    /// <param name="configure">The health-check configuration callback.</param>
    /// <returns><paramref name="configurator"/>.</returns>
    public static IBusRegistrationConfigurator ConfigureHealthCheckOptions(this IBusRegistrationConfigurator configurator,
        Action<IHealthCheckOptionsConfigurator> configure)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(configure);

        configurator.Services.AddOptions<ViciOneServiceBusHealthCheckOptions<IBus>>()
            .Configure(configure)
            .Validate(
                static options => options.Name is null || !string.IsNullOrWhiteSpace(options.Name),
                "Health check for bus 'default': Name must not be empty when specified. Set a non-empty name or leave it unset.")
            .Validate(
                static options => options.MinimalFailureStatus is null || Enum.IsDefined(options.MinimalFailureStatus.Value),
                "Health check for bus 'default': MinimalFailureStatus is not defined. Select a valid HealthStatus value or leave it unset.")
            .Validate(
                static options => options.Tags.All(static tag => !string.IsNullOrWhiteSpace(tag)),
                "Health check for bus 'default': Tags contains an empty value. Remove empty tags before starting the host.")
            .ValidateOnStart();

        return configurator;
    }

    /// <summary>Configures the health-check name, failure-status floor, and tags for a typed bus.</summary>
    /// <typeparam name="TBus">The bus contract that owns the health check.</typeparam>
    /// <param name="configurator">The typed bus registration to configure.</param>
    /// <param name="configure">The health-check configuration callback.</param>
    /// <returns><paramref name="configurator"/>.</returns>
    public static IBusRegistrationConfigurator<TBus> ConfigureHealthCheckOptions<TBus>(
        this IBusRegistrationConfigurator<TBus> configurator,
        Action<IHealthCheckOptionsConfigurator> configure)
        where TBus : class, IBus
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(configure);

        configurator.Services.AddOptions<ViciOneServiceBusHealthCheckOptions<TBus>>()
            .Configure(configure)
            .Validate(
                static options => options.Name is null || !string.IsNullOrWhiteSpace(options.Name),
                $"Health check for bus '{typeof(TBus).FullName}': Name must not be empty when specified. Set a non-empty name or leave it unset.")
            .Validate(
                static options => options.MinimalFailureStatus is null || Enum.IsDefined(options.MinimalFailureStatus.Value),
                $"Health check for bus '{typeof(TBus).FullName}': MinimalFailureStatus is not defined. Select a valid HealthStatus value or leave it unset.")
            .Validate(
                static options => options.Tags.All(static tag => !string.IsNullOrWhiteSpace(tag)),
                $"Health check for bus '{typeof(TBus).FullName}': Tags contains an empty value. Remove empty tags before starting the host.")
            .ValidateOnStart();

        return configurator;
    }
}
