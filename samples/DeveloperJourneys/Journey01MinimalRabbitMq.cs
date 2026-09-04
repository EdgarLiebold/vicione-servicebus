namespace ViciOne.ServiceBus.Samples.DeveloperJourneys;

using Microsoft.Extensions.DependencyInjection;

public static class Journey01MinimalRabbitMq
{
    public static IServiceCollection Configure(IServiceCollection services, string host, string user, string password) =>
        services.AddViciOneServiceBus(configuration =>
        {
            configuration.AddConsumer<SubmitOrderConsumer>();
            configuration.UsingRabbitMq((context, rabbit) =>
            {
                rabbit.Host(host, credentials =>
                {
                    credentials.Username(user);
                    credentials.Password(password);
                });
                rabbit.ConfigureEndpoints(context);
            });
        });
}
