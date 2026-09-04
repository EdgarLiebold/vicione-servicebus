using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.InMemoryTransport.Configuration;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.InMemoryTransport;

public sealed class InMemoryTransportProvider :
    Agent,
    IInMemoryTransportProvider
{
    readonly IInMemoryHostConfiguration _hostConfiguration;
    readonly IMessageFabric<InMemoryTransportContext, InMemoryTransportMessage> _messageFabric;
    readonly IInMemoryTopologyConfiguration _topologyConfiguration;

    public InMemoryTransportProvider(IInMemoryHostConfiguration hostConfiguration, IInMemoryTopologyConfiguration topologyConfiguration)
    {
        _hostConfiguration = hostConfiguration;
        _topologyConfiguration = topologyConfiguration;

        _messageFabric = new MessageFabric<InMemoryTransportContext, InMemoryTransportMessage>(hostConfiguration.QueueCapacity);

        SetReady();
    }

    public IMessageFabric<InMemoryTransportContext, InMemoryTransportMessage> MessageFabric => _messageFabric;

    public async Task<ISendTransport> CreateSendTransportAsync(ReceiveEndpointContext receiveEndpointContext, Uri address, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); LogContext.SetCurrentIfNull(_hostConfiguration.LogContext);

        var endpointAddress = new InMemoryEndpointAddress(_hostConfiguration.HostAddress, address);

        TransportLogMessages.CreateSendTransport(address);

        IMessageExchange<InMemoryTransportMessage> exchange = _messageFabric.GetExchange(this, endpointAddress.Name, endpointAddress.ExchangeType);

        var context = new InMemorySendTransportContext(_hostConfiguration, receiveEndpointContext, exchange, _messageFabric.DelayProvider);

        return new SendTransport<PipeContext>(context);
    }

    public Uri NormalizeAddress(Uri address)
    {
        return new InMemoryEndpointAddress(_hostConfiguration.HostAddress, address);
    }

    public Task<ISendTransport> CreatePublishTransportAsync<T>(ReceiveEndpointContext receiveEndpointContext, Uri publishAddress, CancellationToken cancellationToken = default)
        where T : class
    {
        IInMemoryMessagePublishTopologyConfigurator<T> publishTopology = _topologyConfiguration.Publish.GetMessageTopology<T>();

        ApplyTopologyToMessageFabric(publishTopology);

        return CreateSendTransportAsync(receiveEndpointContext, publishAddress, cancellationToken: cancellationToken);
    }

    public void Probe(ProbeContext context)
    {
        _messageFabric.Probe(context);
    }

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
