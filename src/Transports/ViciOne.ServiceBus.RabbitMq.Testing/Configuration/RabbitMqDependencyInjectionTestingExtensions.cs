using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ViciOne.ServiceBus.Testing;

#nullable enable
namespace ViciOne.ServiceBus.RabbitMq.Testing;

/// <summary>
/// Provides extension methods for rabbit mq dependency injection testing.
/// </summary>
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
        var descriptor = services.FirstOrDefault(x => x.ServiceType == typeof(IBus));
        if (descriptor != null)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("RabbitMQ", "unknown", "RabbitMQ Test Options must be configured before calling AddViciOneServiceBus", "Correct the named configuration before starting the host"));

        services.AddOptions<RabbitMqTestHarnessOptions>()
            .Configure(options =>
            {
                configure?.Invoke(options);
            })
            .Validate(
                static options => !options.ForceCleanRootVirtualHost || options.CleanVirtualHost,
                "RabbitMQ test harness for bus 'default': ForceCleanRootVirtualHost requires CleanVirtualHost. Enable CleanVirtualHost or disable the force flag.")
            .ValidateOnStart();

        services.AddHostedService<RabbitMqTestHarnessHostedService>();

        return services;
    }
}
