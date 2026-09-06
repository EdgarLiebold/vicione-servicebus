using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ViciOne.ServiceBus.Testing;

namespace ViciOne.ServiceBus.RabbitMq.Testing;

/// <summary>Registers RabbitMQ test-harness startup preparation in dependency injection.</summary>
public static class RabbitMqDependencyInjectionTestingExtensions
{
    /// <summary>Configures broker preparation performed before the RabbitMQ test host starts.</summary>
    /// <param name="services">The dependency-injection service collection.</param>
    /// <param name="configure">An optional callback that selects virtual-host creation, cleanup, and post-creation configuration.</param>
    /// <returns>The same service collection, for fluent registration.</returns>
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
