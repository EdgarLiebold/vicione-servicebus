using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Providers.Transports;

namespace ViciOne.ServiceBus.Samples.DeveloperJourneys;

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
