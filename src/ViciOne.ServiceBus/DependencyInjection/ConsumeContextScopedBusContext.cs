using System;
using ViciOne.ServiceBus.Clients;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>
/// Provides a consume context scoped bus context implementation.
/// </summary>
public class ConsumeContextScopedBusContext :
    ScopedBusContext
{
    readonly ScopedClientFactory _clientFactory;
    readonly ConsumeContext _context;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="clientFactory">The client factory value.</param>
    public ConsumeContextScopedBusContext(ConsumeContext context, IClientFactory clientFactory)
    {
        _context = context;
        _clientFactory = new ScopedClientFactory(clientFactory, context);
    }

    /// <summary>
    /// Gets the send endpoint provider value.
    /// </summary>
    public ISendEndpointProvider SendEndpointProvider => _context;

    /// <summary>
    /// Gets the publish endpoint value.
    /// </summary>
    public IPublishEndpoint PublishEndpoint => _context;

    /// <summary>
    /// Gets the client factory value.
    /// </summary>
    public IScopedClientFactory ClientFactory => _clientFactory;
}


/// <summary>
/// Provides a consume context scoped bus context implementation.
/// </summary>
/// <typeparam name="TBus">The t bus type.</typeparam>
public class ConsumeContextScopedBusContext<TBus> :
    ScopedBusContext
    where TBus : class, IBus
{
    readonly TBus _bus;
    readonly ScopedClientFactory _clientFactory;
    readonly ConsumeContext _context;
    readonly IServiceProvider _provider;
    IPublishEndpoint? _publishEndpoint;
    ISendEndpointProvider? _sendEndpointProvider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="bus">The bus value.</param>
    /// <param name="context">The operation context.</param>
    /// <param name="clientFactory">The client factory value.</param>
    /// <param name="provider">The service provider.</param>
    public ConsumeContextScopedBusContext(TBus bus, ConsumeContext context, IClientFactory clientFactory, IServiceProvider provider)
    {
        _bus = bus;
        _context = context;
        _provider = provider;
        _clientFactory = new ScopedClientFactory(clientFactory, context);
    }

    /// <summary>
    /// Gets the send endpoint provider value.
    /// </summary>
    public ISendEndpointProvider SendEndpointProvider
    {
        get { return _sendEndpointProvider ??= new ScopedConsumeSendEndpointProvider(_bus, _context, _provider); }
    }

    /// <summary>
    /// Gets the publish endpoint value.
    /// </summary>
    public IPublishEndpoint PublishEndpoint
    {
        get { return _publishEndpoint ??= new PublishEndpoint(new ScopedConsumePublishEndpointProvider(_bus, _context, _provider)); }
    }

    /// <summary>
    /// Gets the client factory value.
    /// </summary>
    public IScopedClientFactory ClientFactory => _clientFactory;
}
