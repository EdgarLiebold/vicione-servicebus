using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.InMemoryTransport.Configuration;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.InMemoryTransport.Runtime;

/// <summary>Owns the message fabric and creates in-memory send transports for one host.</summary>
internal sealed class InMemoryTransportProvider :
    Agent,
    IInMemoryTransportProvider
{
    readonly IInMemoryHostConfiguration _hostConfiguration;
    readonly IMessageFabric<IInMemoryTransportContext, InMemoryTransportMessage> _messageFabric;
    readonly IInMemoryTopologyConfiguration _topologyConfiguration;

    /// <summary>Creates a transport provider for one host configuration.</summary>
    /// <param name="hostConfiguration">The host settings and lifecycle owner.</param>
    /// <param name="topologyConfiguration">The topology applied to the shared message fabric.</param>
    public InMemoryTransportProvider(IInMemoryHostConfiguration hostConfiguration, IInMemoryTopologyConfiguration topologyConfiguration)
    {
        _hostConfiguration = hostConfiguration ?? throw new ArgumentNullException(nameof(hostConfiguration));
        _topologyConfiguration = topologyConfiguration ?? throw new ArgumentNullException(nameof(topologyConfiguration));

        _messageFabric = new MessageFabric<IInMemoryTransportContext, InMemoryTransportMessage>(hostConfiguration.QueueCapacity);

        SetReady();
    }

    /// <summary>Gets the message fabric shared by the host's endpoints.</summary>
    public IMessageFabric<IInMemoryTransportContext, InMemoryTransportMessage> MessageFabric => _messageFabric;

    /// <summary>Creates a send transport for an address within this provider's host boundary.</summary>
    /// <param name="receiveEndpointContext">The endpoint context that supplies serialization and observers.</param>
    /// <param name="address">The destination address.</param>
    /// <param name="cancellationToken">The token that cancels transport acquisition.</param>
    /// <returns>A task that produces the send transport.</returns>
    public Task<ISendTransport> CreateSendTransportAsync(
        ReceiveEndpointContext receiveEndpointContext,
        Uri address,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(receiveEndpointContext);
        ArgumentNullException.ThrowIfNull(address);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<ISendTransport>(cancellationToken);

        LogContext.SetCurrentIfNull(_hostConfiguration.LogContext);

        var endpointAddress = new InMemoryEndpointAddress(_hostConfiguration.HostAddress, address);

        TransportLogMessages.CreateSendTransport(address);

        IMessageExchange<InMemoryTransportMessage> exchange = _messageFabric.GetExchange(this, endpointAddress.Name, endpointAddress.ExchangeType);

        var context = new InMemorySendTransportContext(_hostConfiguration, receiveEndpointContext, exchange, _messageFabric.DelayProvider);

        return Task.FromResult<ISendTransport>(new SendTransport<PipeContext>(context));
    }

    /// <summary>Resolves and validates a destination address against this provider's host.</summary>
    /// <param name="address">The destination address.</param>
    /// <returns>The canonical absolute loopback URI.</returns>
    public Uri NormalizeAddress(Uri address)
    {
        ArgumentNullException.ThrowIfNull(address);
        return new InMemoryEndpointAddress(_hostConfiguration.HostAddress, address);
    }

    /// <summary>Applies publish topology and creates the transport for a message contract.</summary>
    /// <typeparam name="T">The published message contract.</typeparam>
    /// <param name="receiveEndpointContext">The endpoint context that supplies serialization and observers.</param>
    /// <param name="publishAddress">The resolved publish address.</param>
    /// <param name="cancellationToken">The token that cancels transport acquisition.</param>
    /// <returns>A task that produces the publish transport.</returns>
    public Task<ISendTransport> CreatePublishTransportAsync<T>(ReceiveEndpointContext receiveEndpointContext, Uri publishAddress, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(receiveEndpointContext);
        ArgumentNullException.ThrowIfNull(publishAddress);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<ISendTransport>(cancellationToken);

        IInMemoryMessagePublishTopology<T> publishTopology =
            _topologyConfiguration.Publish.GetMessageTopology<T>() as IInMemoryMessagePublishTopology<T>
            ?? throw new InvalidOperationException($"The publish topology for {TypeCache<T>.ShortName} is not an in-memory topology.");

        ApplyTopologyToMessageFabric(publishTopology);

        return CreateSendTransportAsync(receiveEndpointContext, publishAddress, cancellationToken: cancellationToken);
    }

    /// <summary>Adds message-fabric state to a diagnostic probe.</summary>
    /// <param name="context">The probe that receives the state.</param>
    public void Probe(ProbeContext context)
    {
        _messageFabric.Probe(context);
    }

    /// <summary>Stops the message fabric after the provider stops accepting work.</summary>
    /// <param name="context">The stop context and deadline.</param>
    /// <returns>A task that completes when the fabric has stopped.</returns>
    protected override async Task StopAgentAsync(StopContext context)
    {
        await base.StopAgentAsync(context).ConfigureAwait(false);

        await _messageFabric.StopAsync(context).ConfigureAwait(false);
    }

    void ApplyTopologyToMessageFabric<T>(IInMemoryMessagePublishTopology<T> publishTopology)
        where T : class
    {
        publishTopology.Apply(new MessageFabricPublishTopologyBuilder<IInMemoryTransportContext, InMemoryTransportMessage>(this, _messageFabric));
    }
}
