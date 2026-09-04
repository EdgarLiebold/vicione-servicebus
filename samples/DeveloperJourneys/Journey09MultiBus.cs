namespace ViciOne.ServiceBus.Samples.DeveloperJourneys;

using Microsoft.Extensions.DependencyInjection;

public static class Journey09MultiBus
{
    public static IServiceCollection Configure(IServiceCollection services) => services
        .AddViciOneServiceBus<IOrdersBus>("orders-v1", configuration => configuration.UsingInMemory())
        .AddViciOneServiceBus<IBillingBus>("billing-v1", configuration => configuration.UsingInMemory());

    public static Task PublishOnOrdersBus(
        IOrdersBus bus,
        OrderSubmitted message,
        CancellationToken cancellationToken) =>
        bus.Publish(message, cancellationToken);
}
