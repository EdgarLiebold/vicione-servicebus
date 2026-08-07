// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
#nullable enable
namespace ViciOne.ServiceBus
{
    using System;
    using Configuration;
    using Microsoft.Extensions.DependencyInjection;


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
            configurator.AddOptions<ViciOneServiceBusHealthCheckOptions<IBus>>()
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
            configurator.AddOptions<ViciOneServiceBusHealthCheckOptions<T>>()
                .Configure(options =>
                {
                    callback?.Invoke(options);
                });

            return configurator;
        }
    }
}
