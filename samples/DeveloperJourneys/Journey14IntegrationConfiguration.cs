using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.AzureServiceBus;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Samples.DeveloperJourneys;

public static class Journey14IntegrationConfiguration
{
    public static IServiceCollection ConfigureAzureServiceBus(
        IServiceCollection services,
        string connectionString) =>
        services.AddViciOneServiceBus(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.AddConsumer<GetOrderConsumer>();
            configuration.UsingAzureServiceBus((context, azure) =>
            {
                azure.Host(connectionString);
                azure.ConfigureEndpoints(context);
            });
        });
}
