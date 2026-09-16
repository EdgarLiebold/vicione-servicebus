using System;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>
/// Captures the bus context for the current scope as a scoped provider, so that it can be resolved
/// by components at runtime (since MS DI doesn't support runtime configuration of scopes).
/// </summary>
/// <typeparam name="TBus">The bus type.</typeparam>
public class ScopedBusContextProvider<TBus> :
    IScopedBusContextProvider<TBus>
    where TBus : class, IBus
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="bus">The bus.</param>
    /// <param name="clientFactory">The client factory.</param>
    /// <param name="busConsumeContextProvider">The bus consume context provider.</param>
    /// <param name="globalConsumeContextProvider">The global consume context provider.</param>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    public ScopedBusContextProvider(TBus bus, Bind<TBus, IClientFactory> clientFactory,
        Bind<TBus, IScopedConsumeContextProvider> busConsumeContextProvider,
        IScopedConsumeContextProvider globalConsumeContextProvider,
        IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(clientFactory);
        ArgumentNullException.ThrowIfNull(busConsumeContextProvider);
        ArgumentNullException.ThrowIfNull(globalConsumeContextProvider);
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(clientFactory.Value, nameof(clientFactory));
        ArgumentNullException.ThrowIfNull(busConsumeContextProvider.Value, nameof(busConsumeContextProvider));

        if (busConsumeContextProvider.Value.HasContext)
            Context = new ConsumeContextScopedBusContext(busConsumeContextProvider.Value.GetContext(), clientFactory.Value);
        else if (globalConsumeContextProvider.HasContext)
            Context = new ConsumeContextScopedBusContext<TBus>(bus, globalConsumeContextProvider.GetContext(), clientFactory.Value, provider);
        else
            Context = new BusScopedBusContext<TBus>(bus, clientFactory.Value, provider);
    }

    /// <summary>Gets the context.</summary>
    public ScopedBusContext Context { get; }
}
