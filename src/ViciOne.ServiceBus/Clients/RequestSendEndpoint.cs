using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Clients;

/// <summary>Initializes and sends requests through a lazily resolved endpoint.</summary>
/// <typeparam name="TRequest">The request message contract.</typeparam>
internal abstract class RequestSendEndpoint<TRequest> :
    IRequestSendEndpoint<TRequest>
    where TRequest : class
{
    readonly ConsumeContext? _consumeContext;

    /// <summary>Creates a request endpoint with optional consume-context propagation.</summary>
    /// <param name="consumeContext">The consume context whose correlation metadata is propagated, or <see langword="null" />.</param>
    protected RequestSendEndpoint(ConsumeContext? consumeContext)
    {
        _consumeContext = consumeContext;
    }

    /// <summary>Initializes a request from property values and sends it outside any ambient outbox.</summary>
    /// <param name="requestId">The identifier used to match the response.</param>
    /// <param name="values">The values used to initialize the request contract.</param>
    /// <param name="pipe">The request send-context pipeline.</param>
    /// <param name="cancellationToken">Cancels endpoint resolution, initialization, or sending.</param>
    /// <returns>A task containing the initialized request message after transport acceptance.</returns>
    public async Task<TRequest> SendAsync(Guid requestId, object values, IPipe<SendContext<TRequest>> pipe, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(pipe);
        cancellationToken.ThrowIfCancellationRequested();

        ISendEndpoint endpoint = (await GetSendEndpointAsync(cancellationToken).ConfigureAwait(false)).SkipOutbox();

        (var message, IPipe<SendContext<TRequest>> sendPipe) = _consumeContext != null
            ? await MessageInitializerCache<TRequest>.InitializeMessageAsync(_consumeContext, values,
                new ConsumeSendPipeAdapter<TRequest>(_consumeContext, pipe, requestId), cancellationToken: cancellationToken).ConfigureAwait(false)
            : await MessageInitializerCache<TRequest>.InitializeMessageAsync(values, pipe, cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync(message, sendPipe, cancellationToken).ConfigureAwait(false);

        return message;
    }

    /// <summary>Sends an existing request outside any ambient outbox.</summary>
    /// <param name="requestId">The identifier used to match the response.</param>
    /// <param name="message">The request message.</param>
    /// <param name="pipe">The request send-context pipeline.</param>
    /// <param name="cancellationToken">Cancels endpoint resolution or sending.</param>
    /// <returns>A task that completes when the destination transport accepts the request.</returns>
    public async Task SendAsync(Guid requestId, TRequest message, IPipe<SendContext<TRequest>> pipe, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);
        cancellationToken.ThrowIfCancellationRequested();

        ISendEndpoint endpoint = (await GetSendEndpointAsync(cancellationToken).ConfigureAwait(false)).SkipOutbox();

        IPipe<SendContext<TRequest>> consumePipe = _consumeContext != null
            ? new ConsumeSendPipeAdapter<TRequest>(_consumeContext, pipe, requestId)
            : pipe;

        await endpoint.SendAsync(message, consumePipe, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Resolves the endpoint that accepts the request.</summary>
    /// <param name="cancellationToken">Cancels endpoint resolution.</param>
    /// <returns>A task containing the resolved send endpoint.</returns>
    protected abstract Task<ISendEndpoint> GetSendEndpointAsync(CancellationToken cancellationToken);
}
