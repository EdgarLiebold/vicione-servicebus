namespace ViciOne.ServiceBus.Samples.DeveloperJourneys;

using Microsoft.Extensions.DependencyInjection;

public static class Journey05ConsumerAndRetry
{
    public static IServiceCollection Configure(IServiceCollection services) =>
        services.AddViciOneServiceBus(configuration =>
        {
            configuration.AddConsumer<SubmitOrderConsumer, RetryingSubmitOrderDefinition>();
            configuration.UsingInMemory((context, bus) => bus.ConfigureEndpoints(context));
        });

    public sealed class RetryingSubmitOrderDefinition : ConsumerDefinition<SubmitOrderConsumer>
    {
        protected override void ConfigureConsumer(
            IReceiveEndpointConfigurator endpoint,
            IConsumerConfigurator<SubmitOrderConsumer> consumer,
            IRegistrationContext context) =>
            endpoint.UseMessageRetry(retry => retry.Exponential(5, TimeSpan.FromMilliseconds(50), TimeSpan.FromSeconds(5),
                TimeSpan.FromMilliseconds(100)));
    }
}
