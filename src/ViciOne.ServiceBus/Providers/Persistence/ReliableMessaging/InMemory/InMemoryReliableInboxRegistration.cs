using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.Outbox;

namespace ViciOne.ServiceBus.Providers.Persistence;

internal static class InMemoryReliableInboxRegistration
{
    public static void Add<TBus>(IServiceCollection services)
        where TBus : class, IBus
    {
        services.AddScoped<IOutboxContextFactory<InMemoryReliableInboxScope<TBus>>,
            InMemoryReliableInboxContextFactory<TBus>>();
        services.AddSingleton(provider => Bind<TBus>.Create<IConfigureReceiveEndpoint>(
            new InMemoryReliableInboxEndpointConfiguration<TBus>(
                provider.GetRequiredService<Bind<TBus, IBusRegistrationContext>>().Value,
                provider.GetRequiredService<ReliableMessagingPolicy<TBus>>().MaximumDeliveryAttempts)));
    }
}
