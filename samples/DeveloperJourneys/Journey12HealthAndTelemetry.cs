using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.Samples.DeveloperJourneys;

public static class Journey12HealthAndTelemetry
{
    public static IServiceCollection Configure(IServiceCollection services)
    {
        services.AddLogging();
        services.AddMetrics();
        services.AddHealthChecks().AddViciOneReliableMessagingHealthCheck<IOrdersBus>("orders-durable-send");
        return services;
    }
}
