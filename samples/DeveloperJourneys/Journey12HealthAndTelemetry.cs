namespace ViciOne.ServiceBus.Samples.DeveloperJourneys;

using Microsoft.Extensions.DependencyInjection;

public static class Journey12HealthAndTelemetry
{
    public static IServiceCollection Configure(IServiceCollection services)
    {
        services.AddLogging();
        services.AddMetrics();
        services.AddHealthChecks().AddViciOneDurableSenderHealthCheck<IOrdersBus>("orders-durable-send");
        return services;
    }
}
