// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
#nullable enable
namespace ViciOne.ServiceBus
{
    using System;
    using System.Linq;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Testing;


    public static class RabbitMqDependencyInjectionTestingExtensions
    {
        /// <summary>
        /// Specify the test and/or the test inactivity timeouts that should be used by the test harness.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        public static IServiceCollection ConfigureRabbitMqTestOptions(this IServiceCollection services, Action<RabbitMqTestHarnessOptions>? configure)
        {
            var descriptor = services.FirstOrDefault(x => x.ServiceType == typeof(IHostedService) && x.ImplementationType == typeof(ViciOneServiceBusHostedService));
            if (descriptor != null)
                throw new ConfigurationException("RabbitMQ Test Options must be configured before calling AddViciOneServiceBus");

            services.AddOptions<RabbitMqTestHarnessOptions>()
                .Configure(options =>
                {
                    configure?.Invoke(options);
                });

            services.AddHostedService<RabbitMqTestHarnessHostedService>();

            return services;
        }
    }
}
