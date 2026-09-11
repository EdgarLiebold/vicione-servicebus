using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Observables;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Caches addressed send endpoints created by a receive endpoint's transport provider.</summary>
internal sealed class SendEndpointProvider :
    ISendEndpointProvider,
    IMessageRouteProvider,
    IAsyncDisposable
{
    readonly ISendEndpointCache<Uri> _cache;
    readonly ReceiveEndpointContext _context;
    readonly SendObservable _observers;
    readonly ISendTransportProvider _provider;
    readonly ISendPipe _sendPipe;

    /// <summary>Initializes the endpoint provider from its transport, observers, receive context, and send pipe.</summary>
    /// <param name="provider">The provider that normalizes addresses and creates send transports.</param>
    /// <param name="observers">The observers connected to created transports.</param>
    /// <param name="context">The receive endpoint context that supplies source metadata.</param>
    /// <param name="sendPipe">The endpoint-level send pipe.</param>
    internal SendEndpointProvider(ISendTransportProvider provider, SendObservable observers, ReceiveEndpointContext context, ISendPipe sendPipe)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _sendPipe = sendPipe ?? throw new ArgumentNullException(nameof(sendPipe));

        _observers = observers ?? throw new ArgumentNullException(nameof(observers));
        _context = context ?? throw new ArgumentNullException(nameof(context));

        _cache = new SendEndpointCache<Uri>();
    }

    /// <summary>Resolves a cached send endpoint for a normalized destination address.</summary>
    /// <param name="address">The absolute or provider-relative destination address.</param>
    /// <param name="cancellationToken">The token that cancels endpoint acquisition.</param>
    /// <returns>A task that produces the addressed send endpoint.</returns>
    public Task<ISendEndpoint> GetSendEndpointAsync(Uri address, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(address);
        cancellationToken.ThrowIfCancellationRequested();

        address = _provider.NormalizeAddress(address)
            ?? throw new InvalidOperationException("The send transport provider returned no normalized address.");

        return _cache.GetSendEndpointAsync(address, CreateSendEndpointAsync, cancellationToken: cancellationToken);
    }

    IMessageRouteTable IMessageRouteProvider.MessageRoutes => _context.MessageRoutes;

    /// <summary>Releases every send endpoint and transport owned by the cache.</summary>
    /// <returns>A value task that completes after cached resources have been disposed.</returns>
    public ValueTask DisposeAsync()
    {
        return _cache.DisposeAsync();
    }

    /// <summary>Subscribes an observer to send notifications emitted by cached endpoints.</summary>
    /// <param name="observer">The observer that receives send notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _observers.Connect(observer);
    }

    async Task<ISendEndpoint> CreateSendEndpointAsync(Uri address, CancellationToken cancellationToken)
    {
        Task<ISendTransport> sendTransportTask = _provider.GetSendTransportAsync(address, cancellationToken)
            ?? throw new InvalidOperationException("The transport provider returned no acquisition task.");
        ISendTransport sendTransport = await sendTransportTask.ConfigureAwait(false)
            ?? throw new InvalidOperationException($"The transport provider returned no send transport for '{address}'.");

        ConnectHandle? handle = null;
        try
        {
            handle = sendTransport.ConnectSendObserver(_observers)
                ?? throw new InvalidOperationException("The send transport returned no observer connection handle.");

            return new SendEndpoint(sendTransport, _context, address, _sendPipe, handle);
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
                throw new AggregateException("Send endpoint creation and transport cleanup failed.", failures);
            }

            throw;
        }
    }
}
