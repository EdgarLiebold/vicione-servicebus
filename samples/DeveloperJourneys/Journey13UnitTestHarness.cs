using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.AzureServiceBus.Testing;
using ViciOne.ServiceBus.EventHubs.Testing;
using ViciOne.ServiceBus.RabbitMq.Testing;
using ViciOne.ServiceBus.Testing;

namespace ViciOne.ServiceBus.Samples.DeveloperJourneys;

public static class Journey13UnitTestHarness
{
    public static IServiceCollection Configure(IServiceCollection services) =>
        services.AddViciOneServiceBusTestHarness(configuration =>
        {
            configuration.AddConsumer<SubmitOrderConsumer>();
            configuration.SetTestTimeouts(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(2));
        });

    public static Task<ITestHarness> StartAsync(
        IServiceProvider provider,
        CancellationToken cancellationToken = default) =>
        provider.StartTestHarnessAsync(cancellationToken: cancellationToken);

    public static IServiceCollection ConfigureProviderHarnesses(IServiceCollection services)
    {
        services.AddAzureServiceBusTestHarness(options => options.CleanNamespaceOnStart = false);
        services.AddRabbitMqTestHarness(options =>
        {
            options.CleanVirtualHostOnStart = false;
            options.CreateVirtualHostIfMissing = false;
        });

        return services;
    }

    public static IReadOnlyList<string> ProviderTestingAssemblies() =>
    [
        typeof(AzureServiceBusTestHarnessOptions).Assembly.GetName().Name!,
        typeof(EventHubTestHarnessExtensions).Assembly.GetName().Name!,
        typeof(RabbitMqTestHarnessOptions).Assembly.GetName().Name!,
    ];
}
