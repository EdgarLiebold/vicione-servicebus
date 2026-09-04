namespace ViciOne.ServiceBus.Samples.DeveloperJourneys;

using Microsoft.Extensions.DependencyInjection;

public static class Journey05ConsumerAndRetry
{
    public static IServiceCollection Configure(IServiceCollection services) =>
        services.AddViciOneServiceBus(configuration =>
        {
            configuration.AddConsumer<SubmitOrderConsumer>(consumer =>
                consumer.UseMessageRetry(retry => retry.Exponential(
                    5,
                    TimeSpan.FromMilliseconds(50),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromMilliseconds(100))));
            configuration.UsingInMemory((context, bus) => bus.ConfigureEndpoints(context));
        });
}
