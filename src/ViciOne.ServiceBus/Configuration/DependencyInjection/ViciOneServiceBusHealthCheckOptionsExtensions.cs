using System;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;

#nullable enable
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
            });

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
            });

        return configurator;
    }
}
