using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;

#nullable enable
namespace ViciOne.ServiceBus.Clients;

public abstract class RequestSendEndpoint<TRequest> :
    IRequestSendEndpoint<TRequest>
    where TRequest : class
{
    readonly ConsumeContext? _consumeContext;

    protected RequestSendEndpoint(ConsumeContext? consumeContext)
    {
        _consumeContext = consumeContext;
    }

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

    public async Task SendAsync(Guid requestId, TRequest message, IPipe<SendContext<TRequest>> pipe, CancellationToken cancellationToken)
    {
        ISendEndpoint endpoint = (await GetSendEndpointAsync().ConfigureAwait(false)).SkipOutbox();

        IPipe<SendContext<TRequest>> consumePipe = _consumeContext != null
            ? new ConsumeSendPipeAdapter<TRequest>(_consumeContext, pipe, requestId)
            : pipe;

        await endpoint.SendAsync(message, consumePipe, cancellationToken).ConfigureAwait(false);
    }

    protected abstract Task<ISendEndpoint> GetSendEndpointAsync();
}
