using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ViciOne.ServiceBus.Testing;

namespace ViciOne.ServiceBus.AzureServiceBus.Testing;

/// <summary>Registers Azure Service Bus test-harness services.</summary>
public static class AzureServiceBusDependencyInjectionTestingExtensions
{
    /// <summary>Registers namespace-cleanup options and startup preparation for Azure Service Bus tests.</summary>
    /// <param name="services">The service collection to configure before registering the bus.</param>
    /// <param name="configure">The callback that configures namespace cleanup.</param>
    /// <returns>The same service collection, for fluent registration.</returns>
    public static IServiceCollection AddAzureServiceBusTestHarness(
        this IServiceCollection services,
        Action<AzureServiceBusTestHarnessOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        if (services.Any(static descriptor => descriptor.ServiceType == typeof(IBus)))
        {
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                    "Azure Service Bus",
                    "unknown",
                    "Azure Service Bus test-harness services must be registered before AddViciOneServiceBus",
                    "Register the test harness before registering the bus"));
        }

        services.AddOptions<AzureServiceBusTestHarnessOptions>()
            .Configure(configure)
            .ValidateOnStart();

        services.AddHostedService<AzureServiceBusTestHarnessHostedService>();

        return services;
    }
}
