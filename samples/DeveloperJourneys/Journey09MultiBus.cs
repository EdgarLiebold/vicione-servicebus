using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.Samples.DeveloperJourneys;

public static class Journey09MultiBus
{
    public static IServiceCollection Configure(IServiceCollection services) => services
        .AddViciOneServiceBus<IOrdersBus>("orders-v1", configuration => configuration.UsingInMemory())
        .AddViciOneServiceBus<IBillingBus>("billing-v1", configuration => configuration.UsingInMemory());

    public static Task PublishOnOrdersBusAsync(
        IOrdersBus bus,
        OrderSubmitted message,
        CancellationToken cancellationToken = default) =>
        bus.PublishAsync(message, cancellationToken);
}
