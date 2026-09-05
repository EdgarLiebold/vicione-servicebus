using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Samples.DeveloperJourneys;

public static class Journey16MessageLimits
{
    public static IServiceCollection Configure(IServiceCollection services) =>
        services.AddViciOneServiceBus(configuration =>
        {
            configuration.Limits(new MessageLimits
            {
                MaxBodyBytes = 512 * 1024,
                MaxEnvelopeBytes = 1024 * 1024,
                MaxJsonDepth = 32,
                WarnAboveBytes = 384 * 1024,
            });
            configuration.AddConsumer<SubmitOrderConsumer>();
            configuration.UsingInMemory((context, transport) => transport.ConfigureEndpoints(context));
        });
}
