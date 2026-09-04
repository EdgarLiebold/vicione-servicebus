namespace ViciOne.ServiceBus.Samples.DeveloperJourneys;

using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Testing;

public static class Journey13UnitTestHarness
{
    public static IServiceCollection Configure(IServiceCollection services) =>
        services.AddViciOneServiceBusTestHarness(configuration =>
        {
            configuration.AddConsumer<SubmitOrderConsumer>();
            configuration.SetTestTimeouts(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(2));
        });

    public static Task<ITestHarness> Start(IServiceProvider provider) => provider.StartTestHarness();
}
