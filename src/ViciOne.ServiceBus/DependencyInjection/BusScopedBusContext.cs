using System;
using ViciOne.ServiceBus.Clients;
using ViciOne.ServiceBus.Transports;

#nullable enable
namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>
/// Provides a bus scoped bus context implementation.
/// </summary>
/// <typeparam name="TBus">The t bus type.</typeparam>
public class BusScopedBusContext<TBus> :
    ScopedBusContext
    where TBus : class, IBus
{
    readonly TBus _bus;
    readonly IClientFactory _clientFactory;
    readonly IServiceProvider _provider;
    IPublishEndpoint? _publishEndpoint;
    IScopedClientFactory? _scopedClientFactory;
    ISendEndpointProvider? _sendEndpointProvider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="bus">The bus value.</param>
    /// <param name="clientFactory">The client factory value.</param>
    /// <param name="provider">The service provider.</param>
    public BusScopedBusContext(TBus bus, IClientFactory clientFactory, IServiceProvider provider)
    {
        _bus = bus;
        _clientFactory = clientFactory;
        _provider = provider;
    }

    /// <summary>
    /// Gets the send endpoint provider value.
    /// </summary>
    public ISendEndpointProvider SendEndpointProvider
    {
        get { return _sendEndpointProvider ??= new ScopedSendEndpointProvider(_bus, _provider); }
    }

    /// <summary>
    /// Gets the publish endpoint value.
    /// </summary>
    public IPublishEndpoint PublishEndpoint
    {
        get { return _publishEndpoint ??= new PublishEndpoint(new ScopedPublishEndpointProvider(_bus, _provider)); }
    }

    /// <summary>
    /// Gets the client factory value.
    /// </summary>
    public IScopedClientFactory ClientFactory
    {
        get
        {
            return _scopedClientFactory ??=
                new ScopedClientFactory(new ClientFactory(new ScopedClientFactoryContext(_clientFactory, _provider)), null);
        }
    }
}


/// <summary>
/// Provides a bus scoped bus context implementation.
/// </summary>
public class BusScopedBusContext :
    ScopedBusContext
{
    readonly IClientFactory _clientFactory;
    readonly IServiceProvider _provider;
    readonly ScopedBusContext _scopedBusContext;
    IScopedClientFactory? _scopedClientFactory;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="scopedBusContext">The scoped bus context value.</param>
    /// <param name="clientFactory">The client factory value.</param>
    /// <param name="provider">The service provider.</param>
    public BusScopedBusContext(ScopedBusContext scopedBusContext, IClientFactory clientFactory, IServiceProvider provider)
    {
        _scopedBusContext = scopedBusContext;
        _clientFactory = clientFactory;
        _provider = provider;
    }

    /// <summary>
    /// Gets the send endpoint provider value.
    /// </summary>
    public ISendEndpointProvider SendEndpointProvider => _scopedBusContext.SendEndpointProvider;

    /// <summary>
    /// Gets the publish endpoint value.
    /// </summary>
    public IPublishEndpoint PublishEndpoint => _scopedBusContext.PublishEndpoint;

    /// <summary>
    /// Gets the client factory value.
    /// </summary>
    public IScopedClientFactory ClientFactory
    {
        get
        {
            return _scopedClientFactory ??=
                new ScopedClientFactory(new ClientFactory(new ScopedClientFactoryContext(_clientFactory, _provider)), null);
        }
    }
}
