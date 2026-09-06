using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ViciOne.ServiceBus.Testing;

namespace ViciOne.ServiceBus.AzureServiceBus.Testing;

/// <summary>
/// Provides extension methods for service bus dependency injection testing.
/// </summary>
public static class ServiceBusDependencyInjectionTestingExtensions
{
    /// <summary>
    /// Specify the test and/or the test inactivity timeouts that should be used by the test harness.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configure"></param>
    /// <returns></returns>
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
