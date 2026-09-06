using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ViciOne.ServiceBus.Testing;

namespace ViciOne.ServiceBus.AzureServiceBus.Testing;

/// <summary>Adds Azure Service Bus test-harness services to dependency injection.</summary>
public static class ServiceBusDependencyInjectionTestingExtensions
{
    /// <summary>Registers namespace-cleanup options and the test-harness hosted service.</summary>
    /// <param name="services">The service collection to configure before registering the bus.</param>
    /// <param name="configure">An optional callback that configures namespace cleanup.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection ConfigureServiceBusTestOptions(this IServiceCollection services, Action<AzureServiceBusTestHarnessOptions>? configure)
    {
        var descriptor = services.FirstOrDefault(x => x.ServiceType == typeof(IBus));
        if (descriptor != null)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Azure Service Bus", "unknown", "Azure Service Bus Test Options must be configured before calling AddViciOneServiceBus", "Correct the named configuration before starting the host"));

        services.AddOptions<AzureServiceBusTestHarnessOptions>()
            .Configure(options =>
            {
                configure?.Invoke(options);
            })
            .ValidateOnStart();

        services.AddHostedService<AzureServiceBusTestHarnessHostedService>();

        return services;
    }
}
