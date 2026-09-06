using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.EventHubs.Configuration;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Resolves, caches, observes, and disposes producers for Event Hubs endpoint addresses.</summary>
public class EventHubProducerProvider :
    IEventHubProducerProvider,
    IAsyncDisposable
{
    readonly IBusInstance _busInstance;
    readonly IEventHubProducerCache<Uri> _cache;
    readonly IEventHubHostConfiguration _hostConfiguration;
    readonly SendObservable _sendObservable;

    /// <summary>Creates a producer provider for an Event Hubs rider.</summary>
    /// <param name="hostConfiguration">The Event Hubs host configuration used to build send transports.</param>
    /// <param name="busInstance">The bus instance supplying host and topology services.</param>
    public EventHubProducerProvider(IEventHubHostConfiguration hostConfiguration, IBusInstance busInstance)
    {
        _hostConfiguration = hostConfiguration;
        _busInstance = busInstance;
        _cache = new EventHubProducerCache<Uri>();
        _sendObservable = new SendObservable();
    }

    /// <summary>Disposes the producer cache and all producers it owns.</summary>
    /// <returns>A task that completes after cached producers are disposed.</returns>
    public ValueTask DisposeAsync()
    {
        return _cache.DisposeAsync();
    }

    /// <summary>Gets or creates the cached producer for an Event Hubs endpoint address.</summary>
    /// <param name="address">The endpoint address to normalize against the bus host.</param>
    /// <param name="cancellationToken">Cancels cache lookup or producer creation.</param>
    /// <returns>A task whose result is the endpoint producer.</returns>
    public Task<IEventHubProducer> GetProducerAsync(Uri address, CancellationToken cancellationToken = default)
    {
        return _cache.GetProducerAsync(address, CreateProducerAsync, cancellationToken: cancellationToken);
    }

    /// <summary>Connects an observer to producers created by this provider.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
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
