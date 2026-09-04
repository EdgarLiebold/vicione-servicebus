namespace ViciOne.ServiceBus.Samples.DeveloperJourneys;

using Microsoft.Extensions.DependencyInjection;

public static class Journey10PartitionedConsumer
{
    public static IServiceCollection Configure(IServiceCollection services) =>
        services.AddViciOneServiceBus(configuration =>
        {
            configuration.AddConsumer<SubmitOrderConsumer>(consumer =>
                consumer.UsePartitionedConcurrency<SubmitOrder, Guid>(
                    partitionCount: 16,
                    static message => message.CustomerId));
            configuration.UsingInMemory((context, bus) => bus.ConfigureEndpoints(context));
        });
}
