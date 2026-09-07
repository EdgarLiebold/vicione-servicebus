using System;
using Microsoft.EntityFrameworkCore;
using ViciOne.ServiceBus.Clients;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Middleware.Outbox;
using ViciOne.ServiceBus.Providers.Persistence;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

internal sealed class EntityFrameworkConsumeContextScopedBusContext<TBus, TDbContext> :
    EntityFrameworkScopedBusContext<TBus, TDbContext>
    where TBus : class, IBus
    where TDbContext : DbContext
{
    readonly TBus _bus;
    readonly IClientFactory _clientFactory;
    readonly ConsumeContext _consumeContext;
    readonly IServiceProvider _provider;

    public EntityFrameworkConsumeContextScopedBusContext(TBus bus, TDbContext dbContext, IBusOutboxNotification<EntityFrameworkBusOutboxScope<TBus, TDbContext>> notification, IClientFactory clientFactory,
        IServiceProvider provider, ConsumeContext consumeContext, TimeProvider timeProvider,
        BusPersistenceIdentity<TBus> persistenceIdentity)
        : base(bus, dbContext, notification, clientFactory, provider, timeProvider, persistenceIdentity)
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _consumeContext = consumeContext ?? throw new ArgumentNullException(nameof(consumeContext));
    }

    protected override IPublishEndpointProvider GetPublishEndpointProvider()
    {
        return new ScopedConsumePublishEndpointProvider(_bus, _consumeContext, _provider);
    }

    protected override ISendEndpointProvider GetSendEndpointProvider()
    {
        return new ScopedConsumeSendEndpointProvider(_bus, _consumeContext, _provider);
    }

    protected override ScopedClientFactory GetClientFactory()
    {
        return new ScopedClientFactory(new ClientFactory(new ScopedClientFactoryContext(_clientFactory, _provider)), _consumeContext);
    }
}
