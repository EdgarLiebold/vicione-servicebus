using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Observables;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Caches message-specific publish endpoints created by a transport provider.</summary>
internal sealed class PublishEndpointProvider :
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

    /// <summary>Initializes a publish endpoint provider over transport topology and an owned cache.</summary>
    /// <param name="transportProvider">The provider that creates publish transports.</param>
    /// <param name="hostAddress">The host address used to resolve relative publish addresses.</param>
    /// <param name="publishObservers">The observers connected to publish transports.</param>
    /// <param name="context">The receive endpoint context that supplies source metadata.</param>
    /// <param name="publishPipe">The publish pipeline applied to each send context.</param>
    /// <param name="publishTopology">The topology used to resolve message destinations.</param>
    internal PublishEndpointProvider(IPublishTransportProvider transportProvider, Uri hostAddress, PublishObservable publishObservers,
        ReceiveEndpointContext context, IPublishPipe publishPipe, IPublishTopology publishTopology)
    {
        _transportProvider = transportProvider ?? throw new ArgumentNullException(nameof(transportProvider));
        _hostAddress = hostAddress ?? throw new ArgumentNullException(nameof(hostAddress));
        _publishTopology = publishTopology ?? throw new ArgumentNullException(nameof(publishTopology));
        _publishObservers = publishObservers ?? throw new ArgumentNullException(nameof(publishObservers));
        _context = context ?? throw new ArgumentNullException(nameof(context));

        _publishPipe = new PipeAdapter(publishPipe ?? throw new ArgumentNullException(nameof(publishPipe)));

        _cache = new SendEndpointCache<Type>();
    }

    /// <summary>Resolves the cached publish endpoint for a message contract.</summary>
    /// <typeparam name="T">The published message contract.</typeparam>
    /// <param name="cancellationToken">The token that cancels endpoint acquisition.</param>
    /// <returns>A task that produces the message contract's publish endpoint.</returns>
    public Task<ISendEndpoint> GetPublishSendEndpointAsync<T>(CancellationToken cancellationToken = default)
        where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _cache.GetSendEndpointAsync(
            typeof(T),
            (_, creationCancellationToken) => CreateSendEndpointAsync<T>(creationCancellationToken),
            cancellationToken: cancellationToken);
    }

    /// <summary>Releases every publish endpoint and transport owned by the cache.</summary>
    /// <returns>A value task that completes after cached resources have been disposed.</returns>
    public ValueTask DisposeAsync()
    {
        return _cache.DisposeAsync();
    }

    /// <summary>Subscribes an observer to publish notifications emitted by cached endpoints.</summary>
    /// <param name="observer">The observer that receives publish notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _publishObservers.Connect(observer);
    }

    async Task<ISendEndpoint> CreateSendEndpointAsync<T>(CancellationToken cancellationToken)
        where T : class
    {
        IMessagePublishTopology<T> messageTopology = _publishTopology.GetMessageTopology<T>()
            ?? throw new InvalidOperationException($"The publish topology returned no topology for '{TypeCache<T>.ShortName}'.");

        if (!messageTopology.TryGetPublishAddress(_hostAddress, out var publishAddress))
            throw new PublishException($"An address for publishing message type {TypeCache<T>.ShortName} was not found.");
        if (publishAddress is null)
            throw new InvalidOperationException($"The publish topology returned no address for '{TypeCache<T>.ShortName}'.");

        Task<ISendTransport> sendTransportTask = _transportProvider.GetPublishTransportAsync<T>(publishAddress, cancellationToken)
            ?? throw new InvalidOperationException("The publish transport provider returned no acquisition task.");
        ISendTransport sendTransport = await sendTransportTask.ConfigureAwait(false)
            ?? throw new InvalidOperationException($"The publish transport provider returned no send transport for '{publishAddress}'.");

        ConnectHandle? handle = null;
        try
        {
            handle = sendTransport.ConnectSendObserver(_publishObservers)
                ?? throw new InvalidOperationException("The publish transport returned no observer connection handle.");
            return new SendEndpoint(sendTransport, _context, publishAddress, _publishPipe, handle);
        }
        catch (Exception creationException)
        {
            IReadOnlyList<Exception> cleanupFailures = await SendEndpointResourceRelease
                .CollectFailuresAsync(handle, sendTransport)
                .ConfigureAwait(false);
            if (cleanupFailures.Count > 0)
            {
                var failures = new List<Exception>(cleanupFailures.Count + 1) { creationException };
                failures.AddRange(cleanupFailures);
                throw new AggregateException("Publish endpoint creation and transport cleanup failed.", failures);
            }

            throw;
        }
    }


    sealed class PipeAdapter :
        ISendPipe
    {
        readonly IPublishPipe _publishPipe;

        public PipeAdapter(IPublishPipe publishPipe)
        {
            _publishPipe = publishPipe ?? throw new ArgumentNullException(nameof(publishPipe));
        }

        public Task SendAsync<T>(SendContext<T> context, CancellationToken cancellationToken)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(context);
            var publishContext = context.GetPayload<PublishContext<T>>();

            cancellationToken.ThrowIfCancellationRequested();
            return _publishPipe.SendAsync(publishContext, cancellationToken)
                ?? throw new InvalidOperationException("The publish pipe returned no send task.");
        }

        public void Probe(ProbeContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            _publishPipe.Probe(context);
        }
    }
}
