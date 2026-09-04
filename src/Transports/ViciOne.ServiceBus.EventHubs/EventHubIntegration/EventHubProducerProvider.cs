using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.EventHubs.Configuration;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Provides an event hub producer provider implementation.
/// </summary>
public class EventHubProducerProvider :
    IEventHubProducerProvider,
    IAsyncDisposable
{
    readonly IBusInstance _busInstance;
    readonly IEventHubProducerCache<Uri> _cache;
    readonly IEventHubHostConfiguration _hostConfiguration;
    readonly SendObservable _sendObservable;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="busInstance">The bus instance value.</param>
    public EventHubProducerProvider(IEventHubHostConfiguration hostConfiguration, IBusInstance busInstance)
    {
        _hostConfiguration = hostConfiguration;
        _busInstance = busInstance;
        _cache = new EventHubProducerCache<Uri>();
        _sendObservable = new SendObservable();
    }

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public ValueTask DisposeAsync()
    {
        return _cache.DisposeAsync();
    }

    /// <summary>
    /// Gets producer.
    /// </summary>
    /// <param name="address">The address value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<IEventHubProducer> GetProducerAsync(Uri address, CancellationToken cancellationToken = default)
    {
        return _cache.GetProducerAsync(address, CreateProducerAsync, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Connects send observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
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
