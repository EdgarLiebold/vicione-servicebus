using Microsoft.Extensions.DependencyInjection;
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
}
