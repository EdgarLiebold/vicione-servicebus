namespace ViciOne.ServiceBus.DependencyInjection
{
    using System;
    using Transactions;


    internal abstract class DeferredBusScopedContextProvider<TBus> :
        IScopedBusContextProvider<TBus>
        where TBus : class, IBus
    {
        protected DeferredBusScopedContextProvider(IBus bus, Bind<TBus, IClientFactory> clientFactory,
            Bind<TBus, IScopedConsumeContextProvider> consumeContextProvider, IScopedConsumeContextProvider globalConsumeContextProvider,
            IServiceProvider provider)
        {
            if (bus == null)
                throw new ArgumentNullException(nameof(bus));
            if (clientFactory == null)
                throw new ArgumentNullException(nameof(clientFactory));
            if (consumeContextProvider == null)
                throw new ArgumentNullException(nameof(consumeContextProvider));
            if (globalConsumeContextProvider == null)
                throw new ArgumentNullException(nameof(globalConsumeContextProvider));
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            if (consumeContextProvider.Value.HasContext)
            {
                Context = new ConsumeContextScopedBusContext<IBus>(
                    bus,
                    consumeContextProvider.Value.GetContext(),
                    clientFactory.Value,
                    provider);
            }
            else if (globalConsumeContextProvider.HasContext)
                Context = new ConsumeContextScopedBusContext<IBus>(bus, globalConsumeContextProvider.GetContext(), clientFactory.Value, provider);
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
}
