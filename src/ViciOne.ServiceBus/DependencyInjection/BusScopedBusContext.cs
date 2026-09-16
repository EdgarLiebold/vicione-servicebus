using System;
using ViciOne.ServiceBus.Clients;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Carries state for bus scoped bus operations.</summary>
/// <typeparam name="TBus">The bus type.</typeparam>
public class BusScopedBusContext<TBus> :
    ScopedBusContext
    where TBus : class, IBus
{
    readonly TBus _bus;
    readonly IClientFactory _clientFactory;
    readonly Lazy<IScopedClientFactory> _scopedClientFactory;
    readonly Lazy<IPublishEndpoint> _publishEndpoint;
    readonly IServiceProvider _provider;
    readonly Lazy<ISendEndpointProvider> _sendEndpointProvider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="bus">The bus.</param>
    /// <param name="clientFactory">The client factory.</param>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    public BusScopedBusContext(TBus bus, IClientFactory clientFactory, IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(clientFactory);
        ArgumentNullException.ThrowIfNull(provider);

        _bus = bus;
        _clientFactory = clientFactory;
        _provider = provider;
        _sendEndpointProvider = new Lazy<ISendEndpointProvider>(
            () => new ScopedSendEndpointProvider(_bus, _provider));
        _publishEndpoint = new Lazy<IPublishEndpoint>(
            () => new PublishEndpoint(new ScopedPublishEndpointProvider(_bus, _provider)));
        _scopedClientFactory = new Lazy<IScopedClientFactory>(
            () => new ScopedClientFactory(new ClientFactory(new ScopedClientFactoryContext(_clientFactory, _provider)), null));
    }

    /// <summary>Gets the send endpoint provider.</summary>
    public ISendEndpointProvider SendEndpointProvider
    {
        get { return _sendEndpointProvider.Value; }
    }

    /// <summary>Gets the publish endpoint.</summary>
    public IPublishEndpoint PublishEndpoint
    {
        get { return _publishEndpoint.Value; }
    }

    /// <summary>Gets the client factory.</summary>
    public IScopedClientFactory ClientFactory
    {
        get { return _scopedClientFactory.Value; }
    }
}


/// <summary>Carries state for bus scoped bus operations.</summary>
public class BusScopedBusContext :
    ScopedBusContext
{
    readonly IClientFactory _clientFactory;
    readonly Lazy<IScopedClientFactory> _scopedClientFactory;
    readonly IServiceProvider _provider;
    readonly ScopedBusContext _scopedBusContext;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="scopedBusContext">The scoped bus context.</param>
    /// <param name="clientFactory">The client factory.</param>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    public BusScopedBusContext(ScopedBusContext scopedBusContext, IClientFactory clientFactory, IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(scopedBusContext);
        ArgumentNullException.ThrowIfNull(clientFactory);
        ArgumentNullException.ThrowIfNull(provider);

        _scopedBusContext = scopedBusContext;
        _clientFactory = clientFactory;
        _provider = provider;
        _scopedClientFactory = new Lazy<IScopedClientFactory>(
            () => new ScopedClientFactory(new ClientFactory(new ScopedClientFactoryContext(_clientFactory, _provider)), null));
    }

    /// <summary>Gets the send endpoint provider.</summary>
    public ISendEndpointProvider SendEndpointProvider => _scopedBusContext.SendEndpointProvider;

    /// <summary>Gets the publish endpoint.</summary>
    public IPublishEndpoint PublishEndpoint => _scopedBusContext.PublishEndpoint;

    /// <summary>Gets the client factory.</summary>
    public IScopedClientFactory ClientFactory
    {
        get { return _scopedClientFactory.Value; }
    }
}
