using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ViciOne.ServiceBus.Testing;

namespace ViciOne.ServiceBus.RabbitMq.Testing;

/// <summary>Registers RabbitMQ test-harness services.</summary>
public static class RabbitMqDependencyInjectionTestingExtensions
{
    /// <summary>Registers virtual-host preparation performed before the RabbitMQ test host starts.</summary>
    /// <param name="services">The dependency-injection service collection.</param>
    /// <param name="configure">The callback that selects virtual-host creation, cleanup, and post-cleanup configuration.</param>
    /// <returns>The same service collection, for fluent registration.</returns>
    public static IServiceCollection AddRabbitMqTestHarness(
        this IServiceCollection services,
        Action<RabbitMqTestHarnessOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        if (services.Any(static descriptor => descriptor.ServiceType == typeof(IBus)))
        {
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                    "RabbitMQ",
                    "unknown",
                    "RabbitMQ test-harness services must be registered before AddViciOneServiceBus",
                    "Register the test harness before registering the bus"));
        }

        services.AddOptions<RabbitMqTestHarnessOptions>()
            .Configure(configure)
            .Validate(
                static options => !options.AllowRootVirtualHostCleanup || options.CleanVirtualHostOnStart,
                "RabbitMQ test harness for bus 'default': AllowRootVirtualHostCleanup requires CleanVirtualHostOnStart.")
            .ValidateOnStart();

        services.AddHostedService<RabbitMqTestHarnessHostedService>();

        return services;
    }
}
