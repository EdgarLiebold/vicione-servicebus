using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Observables;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Provides a publish endpoint provider implementation.
/// </summary>
public class PublishEndpointProvider :
    IPublishEndpointProvider,
    IAsyncDisposable
{
    readonly ISendEndpointCache<Type> _cache;
    readonly ReceiveEndpointContext _context;
    readonly Uri _hostAddress;
    readonly PublishObservable _publishObservers;
    readonly IPublishTopology _publishTopology;
    readonly ISendPipe _publishPipe;
    readonly IPublishTransportProvider _transportProvider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="transportProvider">The transport provider value.</param>
    /// <param name="hostAddress">The host address value.</param>
    /// <param name="publishObservers">The publish observers value.</param>
    /// <param name="context">The operation context.</param>
    /// <param name="publishPipe">The publish pipe value.</param>
    /// <param name="publishTopology">The publish topology value.</param>
    public PublishEndpointProvider(IPublishTransportProvider transportProvider, Uri hostAddress, PublishObservable publishObservers,
        ReceiveEndpointContext context, IPublishPipe publishPipe, IPublishTopology publishTopology)
    {
        _transportProvider = transportProvider;
        _hostAddress = hostAddress;
        _publishTopology = publishTopology;
        _publishObservers = publishObservers;
        _context = context;

        _publishPipe = new PipeAdapter(publishPipe);

        _cache = new SendEndpointCache<Type>();
    }

    /// <summary>
    /// Gets publish send endpoint.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<ISendEndpoint> GetPublishSendEndpointAsync<T>(CancellationToken cancellationToken = default)
        where T : class
    {
        return _cache.GetSendEndpointAsync(typeof(T), type => CreateSendEndpointAsync<T>(), cancellationToken: cancellationToken);
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
    /// Connects publish observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
    {
        return _publishObservers.Connect(observer);
    }

    Task<ISendEndpoint> CreateSendEndpointAsync<T>()
        where T : class
    {
        IMessagePublishTopology<T> messageTopology = _publishTopology.GetMessageTopology<T>();

        if (!messageTopology.TryGetPublishAddress(_hostAddress, out var publishAddress))
            throw new PublishException($"An address for publishing message type {TypeCache<T>.ShortName} was not found.");

        Task<ISendTransport> sendTransportTask = _transportProvider.GetPublishTransportAsync<T>(publishAddress);
        if (sendTransportTask.Status == TaskStatus.RanToCompletion)
        {
            var sendTransport = sendTransportTask.Result;

            var sendEndpoint = new SendEndpoint(sendTransport, _context, publishAddress, _publishPipe, sendTransport.ConnectSendObserver(_publishObservers));

            return Task.FromResult<ISendEndpoint>(sendEndpoint);
        }

        async Task<ISendEndpoint> CreateAsync()
        {
            var sendTransport = await sendTransportTask.ConfigureAwait(false);

            return new SendEndpoint(sendTransport, _context, publishAddress, _publishPipe, sendTransport.ConnectSendObserver(_publishObservers));
        }

        return CreateAsync();
    }


    class PipeAdapter :
        ISendPipe
    {
        readonly IPublishPipe _publishPipe;

        public PipeAdapter(IPublishPipe publishPipe)
        {
            _publishPipe = publishPipe;
        }

        public Task SendAsync<T>(SendContext<T> context, CancellationToken cancellationToken)
            where T : class
        {
            var publishContext = context.GetPayload<PublishContext<T>>();

            return _publishPipe.SendAsync(publishContext, cancellationToken);
        }

        public void Probe(ProbeContext context)
        {
            _publishPipe.Probe(context);
        }
    }
}
