using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.InMemoryTransport.Configuration;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.InMemoryTransport;

/// <summary>Provides in memory transport services.</summary>
public sealed class InMemoryTransportProvider :
    Agent,
    IInMemoryTransportProvider
{
    readonly IInMemoryHostConfiguration _hostConfiguration;
    readonly IMessageFabric<InMemoryTransportContext, InMemoryTransportMessage> _messageFabric;
    readonly IInMemoryTopologyConfiguration _topologyConfiguration;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="hostConfiguration">The host configuration.</param>
    /// <param name="topologyConfiguration">The topology configuration.</param>
    public InMemoryTransportProvider(IInMemoryHostConfiguration hostConfiguration, IInMemoryTopologyConfiguration topologyConfiguration)
    {
        _hostConfiguration = hostConfiguration;
        _topologyConfiguration = topologyConfiguration;

        _messageFabric = new MessageFabric<InMemoryTransportContext, InMemoryTransportMessage>(hostConfiguration.QueueCapacity);

        SetReady();
    }

    /// <summary>Gets the message fabric.</summary>
    public IMessageFabric<InMemoryTransportContext, InMemoryTransportMessage> MessageFabric => _messageFabric;

    /// <summary>Creates send transport.</summary>
    /// <param name="receiveEndpointContext">The receive endpoint context.</param>
    /// <param name="address">The address.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the created value.</returns>
    public async Task<ISendTransport> CreateSendTransportAsync(ReceiveEndpointContext receiveEndpointContext, Uri address, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); LogContext.SetCurrentIfNull(_hostConfiguration.LogContext);

        var endpointAddress = new InMemoryEndpointAddress(_hostConfiguration.HostAddress, address);

        TransportLogMessages.CreateSendTransport(address);

        IMessageExchange<InMemoryTransportMessage> exchange = _messageFabric.GetExchange(this, endpointAddress.Name, endpointAddress.ExchangeType);

        var context = new InMemorySendTransportContext(_hostConfiguration, receiveEndpointContext, exchange, _messageFabric.DelayProvider);

        return new SendTransport<PipeContext>(context);
    }

    /// <summary>Normalizes address.</summary>
    /// <param name="address">The address.</param>
    /// <returns>The uri produced by the operation.</returns>
    public Uri NormalizeAddress(Uri address)
    {
        return new InMemoryEndpointAddress(_hostConfiguration.HostAddress, address);
    }

    /// <summary>Creates publish transport.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="receiveEndpointContext">The receive endpoint context.</param>
    /// <param name="publishAddress">The publish address.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the created value.</returns>
    public Task<ISendTransport> CreatePublishTransportAsync<T>(ReceiveEndpointContext receiveEndpointContext, Uri publishAddress, CancellationToken cancellationToken = default)
        where T : class
    {
        IInMemoryMessagePublishTopologyConfigurator<T> publishTopology = _topologyConfiguration.Publish.GetMessageTopology<T>();

        ApplyTopologyToMessageFabric(publishTopology);

        return CreateSendTransportAsync(receiveEndpointContext, publishAddress, cancellationToken: cancellationToken);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        _messageFabric.Probe(context);
    }

    /// <summary>Stops agent.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected override async Task StopAgentAsync(StopContext context)
    {
        await base.StopAgentAsync(context).ConfigureAwait(false);

        await _messageFabric.StopAsync(context).ConfigureAwait(false);
    }

    void ApplyTopologyToMessageFabric<T>(IInMemoryMessagePublishTopology<T> publishTopology)
        where T : class
    {
        publishTopology.Apply(new MessageFabricPublishTopologyBuilder<InMemoryTransportContext, InMemoryTransportMessage>(this, _messageFabric));
    }
}
