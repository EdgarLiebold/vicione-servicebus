using System;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides extension methods for vici one service bus health check options.
/// </summary>
public static class ViciOneServiceBusHealthCheckOptionsExtensions
{
    /// <summary>
    /// Configure the health check options for this bus
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="callback"></param>
    /// <returns></returns>
    public static IBusRegistrationConfigurator ConfigureHealthCheckOptions(this IBusRegistrationConfigurator configurator,
        Action<IHealthCheckOptionsConfigurator>? callback)
    {
        configurator.Services.AddOptions<ViciOneServiceBusHealthCheckOptions<IBus>>()
            .Configure(options =>
            {
                callback?.Invoke(options);
            })
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

    /// <summary>
    /// Configure the health check options for this bus
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="callback"></param>
    /// <returns></returns>
    public static IBusRegistrationConfigurator<T> ConfigureHealthCheckOptions<T>(this IBusRegistrationConfigurator<T> configurator,
        Action<IHealthCheckOptionsConfigurator>? callback)
        where T : class, IBus
    {
        configurator.Services.AddOptions<ViciOneServiceBusHealthCheckOptions<T>>()
            .Configure(options =>
            {
                callback?.Invoke(options);
            })
            .Validate(
                static options => options.Name is null || !string.IsNullOrWhiteSpace(options.Name),
                $"Health check for bus '{typeof(T).FullName}': Name must not be empty when specified. Set a non-empty name or leave it unset.")
            .Validate(
                static options => options.MinimalFailureStatus is null || Enum.IsDefined(options.MinimalFailureStatus.Value),
                $"Health check for bus '{typeof(T).FullName}': MinimalFailureStatus is not defined. Select a valid HealthStatus value or leave it unset.")
            .Validate(
                static options => options.Tags.All(static tag => !string.IsNullOrWhiteSpace(tag)),
                $"Health check for bus '{typeof(T).FullName}': Tags contains an empty value. Remove empty tags before starting the host.")
            .ValidateOnStart();

        return configurator;
    }
}
