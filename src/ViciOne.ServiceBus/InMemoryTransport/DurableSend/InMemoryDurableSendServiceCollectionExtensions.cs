using System;
using System.Linq;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.InMemoryTransport;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Registers the volatile InMemory adapter for one typed durable sender.</summary>
public static class InMemoryDurableSendServiceCollectionExtensions
{
    public static IServiceCollection AddViciOneInMemoryDurableSendDispatcher<TBus>(this IServiceCollection services)
        where TBus : class, IBus
    {
        ArgumentNullException.ThrowIfNull(services);

        if (services.Any(static descriptor => descriptor.ServiceType == typeof(IDurableSendDispatcher<TBus>)))
        {
            throw new ConfigurationException(
                $"A durable-send dispatcher is already registered for bus '{typeof(TBus)}'. Exactly one provider adapter is allowed.");
        }

        services.AddSingleton<IDurableSendDispatcher<TBus>, InMemoryDurableSendDispatcher<TBus>>();
        return services;
    }
}
