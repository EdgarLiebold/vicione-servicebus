using System;
using ViciOne.ServiceBus.Clients;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Carries state for consume context scoped bus operations.</summary>
public class ConsumeContextScopedBusContext :
    ScopedBusContext
{
    readonly ScopedClientFactory _clientFactory;
    readonly ConsumeContext _context;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="clientFactory">The client factory.</param>
    public ConsumeContextScopedBusContext(ConsumeContext context, IClientFactory clientFactory)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(clientFactory);

        _context = context;
        _clientFactory = new ScopedClientFactory(clientFactory, context);
    }

    /// <summary>Gets the send endpoint provider.</summary>
    public ISendEndpointProvider SendEndpointProvider => _context;

    /// <summary>Gets the publish endpoint.</summary>
    public IPublishEndpoint PublishEndpoint => _context;

    /// <summary>Gets the client factory.</summary>
    public IScopedClientFactory ClientFactory => _clientFactory;
}


/// <summary>Carries state for consume context scoped bus operations.</summary>
/// <typeparam name="TBus">The bus type.</typeparam>
public class ConsumeContextScopedBusContext<TBus> :
    ScopedBusContext
    where TBus : class, IBus
{
    readonly TBus _bus;
    readonly ScopedClientFactory _clientFactory;
    readonly ConsumeContext _context;
    readonly Lazy<IPublishEndpoint> _publishEndpoint;
    readonly IServiceProvider _provider;
    readonly Lazy<ISendEndpointProvider> _sendEndpointProvider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="bus">The bus.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="clientFactory">The client factory.</param>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    public ConsumeContextScopedBusContext(TBus bus, ConsumeContext context, IClientFactory clientFactory, IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(clientFactory);
        ArgumentNullException.ThrowIfNull(provider);

        _bus = bus;
        _context = context;
        _provider = provider;
        _clientFactory = new ScopedClientFactory(clientFactory, context);
        _sendEndpointProvider = new Lazy<ISendEndpointProvider>(
            () => new ScopedConsumeSendEndpointProvider(_bus, _context, _provider));
        _publishEndpoint = new Lazy<IPublishEndpoint>(
            () => new PublishEndpoint(new ScopedConsumePublishEndpointProvider(_bus, _context, _provider)));
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
    public IScopedClientFactory ClientFactory => _clientFactory;
}
