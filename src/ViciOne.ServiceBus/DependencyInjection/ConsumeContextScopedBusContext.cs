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
    readonly IServiceProvider _provider;
    IPublishEndpoint? _publishEndpoint;
    ISendEndpointProvider? _sendEndpointProvider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="bus">The bus.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="clientFactory">The client factory.</param>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    public ConsumeContextScopedBusContext(TBus bus, ConsumeContext context, IClientFactory clientFactory, IServiceProvider provider)
    {
        _bus = bus;
        _context = context;
        _provider = provider;
        _clientFactory = new ScopedClientFactory(clientFactory, context);
    }

    /// <summary>Gets the send endpoint provider.</summary>
    public ISendEndpointProvider SendEndpointProvider
    {
        get { return _sendEndpointProvider ??= new ScopedConsumeSendEndpointProvider(_bus, _context, _provider); }
    }

    /// <summary>Gets the publish endpoint.</summary>
    public IPublishEndpoint PublishEndpoint
    {
        get { return _publishEndpoint ??= new PublishEndpoint(new ScopedConsumePublishEndpointProvider(_bus, _context, _provider)); }
    }

    /// <summary>Gets the client factory.</summary>
    public IScopedClientFactory ClientFactory => _clientFactory;
}
