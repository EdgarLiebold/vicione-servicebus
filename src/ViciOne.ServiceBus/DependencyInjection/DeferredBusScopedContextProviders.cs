using System;
using ViciOne.ServiceBus.Transactions;

namespace ViciOne.ServiceBus.DependencyInjection;

internal abstract class DeferredBusScopedContextProvider<TBus> :
    IScopedBusContextProvider<TBus>
    where TBus : class, IBus
{
    protected DeferredBusScopedContextProvider(IBus? bus, Bind<TBus, IClientFactory> clientFactory,
        Bind<TBus, IScopedConsumeContextProvider> consumeContextProvider, IScopedConsumeContextProvider globalConsumeContextProvider,
        IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(clientFactory);
        ArgumentNullException.ThrowIfNull(consumeContextProvider);
        ArgumentNullException.ThrowIfNull(globalConsumeContextProvider);
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(clientFactory.Value, nameof(clientFactory));
        ArgumentNullException.ThrowIfNull(consumeContextProvider.Value, nameof(consumeContextProvider));

        if (consumeContextProvider.Value.TryGetContext(out var context))
        {
            Context = new ConsumeContextScopedBusContext<IBus>(
                bus,
                context,
                clientFactory.Value,
                provider);
        }
        else if (globalConsumeContextProvider.TryGetContext(out context))
            Context = new ConsumeContextScopedBusContext<IBus>(bus, context, clientFactory.Value, provider);
        else
            Context = new BusScopedBusContext<IBus>(bus, clientFactory.Value, provider);
    }

    public ScopedBusContext Context { get; }
}


internal sealed class AmbientTransactionScopedBusContextProvider<TBus> :
    DeferredBusScopedContextProvider<TBus>
    where TBus : class, IBus
{
    public AmbientTransactionScopedBusContextProvider(Bind<TBus, IAmbientTransactionBus> bus, Bind<TBus, IClientFactory> clientFactory,
        Bind<TBus, IScopedConsumeContextProvider> consumeContextProvider, IScopedConsumeContextProvider globalConsumeContextProvider,
        IServiceProvider provider)
        : base(bus?.Value, clientFactory, consumeContextProvider, globalConsumeContextProvider, provider)
    {
    }
}


internal sealed class BufferedBusScopedBusContextProvider<TBus> :
    DeferredBusScopedContextProvider<TBus>
    where TBus : class, IBus
{
    public BufferedBusScopedBusContextProvider(Bind<TBus, IBufferedBus> bus, Bind<TBus, IClientFactory> clientFactory,
        Bind<TBus, IScopedConsumeContextProvider> consumeContextProvider, IScopedConsumeContextProvider globalConsumeContextProvider,
        IServiceProvider provider)
        : base(bus?.Value, clientFactory, consumeContextProvider, globalConsumeContextProvider, provider)
    {
    }
}
