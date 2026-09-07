using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Clients;

/// <summary>Provides an endpoint for request send.</summary>
/// <typeparam name="TRequest">The request type.</typeparam>
internal abstract class RequestSendEndpoint<TRequest> :
    IRequestSendEndpoint<TRequest>
    where TRequest : class
{
    readonly ConsumeContext? _consumeContext;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="consumeContext">The consume context.</param>
    protected RequestSendEndpoint(ConsumeContext? consumeContext)
    {
        _consumeContext = consumeContext;
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="requestId">The request id.</param>
    /// <param name="values">The values.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the send outcome.</returns>
    public async Task<TRequest> SendAsync(Guid requestId, object values, IPipe<SendContext<TRequest>> pipe, CancellationToken cancellationToken)
    {
        ISendEndpoint endpoint = (await GetSendEndpointAsync().ConfigureAwait(false)).SkipOutbox();

        (var message, IPipe<SendContext<TRequest>> sendPipe) = _consumeContext != null
            ? await MessageInitializerCache<TRequest>.InitializeMessageAsync(_consumeContext, values,
                new ConsumeSendPipeAdapter<TRequest>(_consumeContext, pipe, requestId), cancellationToken: cancellationToken).ConfigureAwait(false)
            : await MessageInitializerCache<TRequest>.InitializeMessageAsync(values, pipe, cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync(message, sendPipe, cancellationToken).ConfigureAwait(false);

        return message;
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="requestId">The request id.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SendAsync(Guid requestId, TRequest message, IPipe<SendContext<TRequest>> pipe, CancellationToken cancellationToken)
    {
        ISendEndpoint endpoint = (await GetSendEndpointAsync().ConfigureAwait(false)).SkipOutbox();

        IPipe<SendContext<TRequest>> consumePipe = _consumeContext != null
            ? new ConsumeSendPipeAdapter<TRequest>(_consumeContext, pipe, requestId)
            : pipe;

        await endpoint.SendAsync(message, consumePipe, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Gets send endpoint.</summary>
    /// <returns>A task that produces the requested value.</returns>
    protected abstract Task<ISendEndpoint> GetSendEndpointAsync();
}
