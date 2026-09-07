using System;
using Microsoft.EntityFrameworkCore;
using ViciOne.ServiceBus.Clients;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Middleware.Outbox;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

internal sealed class EntityFrameworkTransactionalConsumeContextScopedBusContext<TBus, TDbContext> :
    EntityFrameworkTransactionalScopedBusContext<TBus, TDbContext>
    where TBus : class, IBus
    where TDbContext : DbContext
{
    readonly TBus _bus;
    readonly IClientFactory _clientFactory;
    readonly ConsumeContext _consumeContext;
    readonly IServiceProvider _provider;

    public EntityFrameworkTransactionalConsumeContextScopedBusContext(
        TBus bus,
        TDbContext dbContext,
        IBusOutboxNotification<EntityFrameworkBusOutboxScope<TBus, TDbContext>> notification,
        IClientFactory clientFactory,
        IServiceProvider provider,
        ConsumeContext consumeContext,
        TimeProvider timeProvider,
        BusPersistenceIdentity<TBus> persistenceIdentity)
        : base(bus, dbContext, notification, clientFactory, provider, timeProvider, persistenceIdentity)
    {
        _bus = bus;
        _clientFactory = clientFactory;
        _provider = provider;
        _consumeContext = consumeContext;
    }

    protected override IPublishEndpointProvider GetPublishEndpointProvider() =>
        new ScopedConsumePublishEndpointProvider(_bus, _consumeContext, _provider);

    protected override ISendEndpointProvider GetSendEndpointProvider() =>
        new ScopedConsumeSendEndpointProvider(_bus, _consumeContext, _provider);

    protected override ScopedClientFactory GetClientFactory() =>
        new(new ClientFactory(new ScopedClientFactoryContext(_clientFactory, _provider)), _consumeContext);
}
