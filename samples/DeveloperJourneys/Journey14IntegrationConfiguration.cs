namespace ViciOne.ServiceBus.Samples.DeveloperJourneys;

using Microsoft.Extensions.DependencyInjection;

public static class Journey14IntegrationConfiguration
{
    public static IServiceCollection ConfigureAzureServiceBus(
        IServiceCollection services,
        string connectionString) =>
        services.AddViciOneServiceBus(configuration =>
        {
            configuration.AddConsumer<GetOrderConsumer>();
            configuration.UsingAzureServiceBus((context, azure) =>
            {
                azure.Host(connectionString);
                azure.ConfigureEndpoints(context);
            });
        });
}
