using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.EventHubIntegration.Configuration;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubIntegration;

public class EventHubProducerProvider :
    IEventHubProducerProvider,
    IAsyncDisposable
{
    readonly IBusInstance _busInstance;
    readonly IEventHubProducerCache<Uri> _cache;
    readonly IEventHubHostConfiguration _hostConfiguration;
    readonly SendObservable _sendObservable;

    public EventHubProducerProvider(IEventHubHostConfiguration hostConfiguration, IBusInstance busInstance)
    {
        _hostConfiguration = hostConfiguration;
        _busInstance = busInstance;
        _cache = new EventHubProducerCache<Uri>();
        _sendObservable = new SendObservable();
    }

    public ValueTask DisposeAsync()
    {
        return _cache.DisposeAsync();
    }

    public Task<IEventHubProducer> GetProducerAsync(Uri address, CancellationToken cancellationToken = default)
    {
        return _cache.GetProducerAsync(address, CreateProducerAsync, cancellationToken: cancellationToken);
    }

    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        return _sendObservable.Connect(observer);
    }

    Task<IEventHubProducer> CreateProducerAsync(Uri address)
    {
        var topicAddress = NormalizeAddress(_busInstance.HostConfiguration.HostAddress, address);
        var transportContext = _hostConfiguration.CreateSendTransportContext(topicAddress.EventHubName, _busInstance);
        IEventHubProducer producer = new EventHubProducer(transportContext, transportContext.ConnectSendObserver(_sendObservable));
        return Task.FromResult(producer);
    }

    static EventHubEndpointAddress NormalizeAddress(Uri hostAddress, Uri address)
    {
        return new EventHubEndpointAddress(hostAddress, address);
    }
}
