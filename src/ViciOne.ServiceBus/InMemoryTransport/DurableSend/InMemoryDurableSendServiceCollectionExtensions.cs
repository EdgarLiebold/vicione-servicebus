using System;
using System.ComponentModel;
using System.Linq;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.DurableSend;
using ViciOne.ServiceBus.InMemoryTransport;
using ViciOne.ServiceBus.ProviderAbstractions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Registers the volatile InMemory adapter for one typed durable sender.</summary>
public static class InMemoryDurableSendServiceCollectionExtensions
{
    /// <summary>
    /// Selects the volatile in-memory store for tests and local process-only hosts. It does not provide restart durability.
    /// </summary>
    public static IDurableSenderConfigurator UseInMemoryStore(this IDurableSenderConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        if (configurator is not IDurableSenderProviderConfigurator provider)
            throw new ConfigurationException("The Durable Sender configurator does not expose the provider registration contract.");

        provider.UseStore(typeof(InMemoryDurableSendStore<>).MakeGenericType(provider.BusType));
        return configurator;
    }

    /// <summary>Low-level adapter registration. InMemory bus configuration adds this capability automatically.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
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
