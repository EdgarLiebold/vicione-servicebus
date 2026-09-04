using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Initializers;

#nullable enable
namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Generalized proxy for ISendEndpoint to intercept pipe/context
/// </summary>
public abstract class SendEndpointProxy :
    ITransportSendEndpoint
{
    readonly ISendEndpoint _endpoint;
    readonly ITransportSendEndpoint? _transportEndpoint;

    protected SendEndpointProxy(ISendEndpoint endpoint)
    {
        _endpoint = endpoint;
        _transportEndpoint = endpoint as ITransportSendEndpoint;
    }

    public ISendEndpoint Endpoint => _endpoint;

    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        return _endpoint.ConnectSendObserver(observer);
    }

    public Task<SendContext<T>> CreateSendContextAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        return _transportEndpoint?.CreateSendContextAsync(message, GetPipeProxy(pipe), cancellationToken)
            ?? throw new InvalidOperationException("The endpoint does not have a valid transport");
    }

    public virtual Task SendAsync<T>(T message, CancellationToken cancellationToken)
        where T : class
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        return _endpoint.SendAsync(message, GetPipeProxy<T>(), cancellationToken);
    }

    public virtual Task SendAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));

        return _endpoint.SendAsync(message, GetPipeProxy(pipe), cancellationToken);
    }

    public virtual Task SendAsync<T>(T message, IPipe<SendContext> pipe, CancellationToken cancellationToken)
        where T : class
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));

        return _endpoint.SendAsync(message, GetPipeProxy<T>(pipe), cancellationToken);
    }

    public Task SendAsync(object message, CancellationToken cancellationToken)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var messageType = message.GetType();

        return SendEndpointConverterCache.SendAsync(this, message, messageType, cancellationToken);
    }

    public Task SendAsync(object message, Type messageType, CancellationToken cancellationToken)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (messageType == null)
            throw new ArgumentNullException(nameof(messageType));

        return SendEndpointConverterCache.SendAsync(this, message, messageType, cancellationToken);
    }

    public Task SendAsync(object message, IPipe<SendContext> pipe, CancellationToken cancellationToken)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));

        var messageType = message.GetType();

        return SendEndpointConverterCache.SendAsync(this, message, messageType, pipe, cancellationToken);
    }

    public Task SendAsync(object message, Type messageType, IPipe<SendContext> pipe, CancellationToken cancellationToken)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (messageType == null)
            throw new ArgumentNullException(nameof(messageType));
        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));

        return SendEndpointConverterCache.SendAsync(this, message, messageType, pipe, cancellationToken);
    }

    public async Task SendAsync<T>(object values, CancellationToken cancellationToken)
        where T : class
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        (var message, IPipe<SendContext<T>> sendPipe) =
            await MessageInitializerCache<T>.InitializeMessageAsync(values, GetPipeProxy<T>(), cancellationToken).ConfigureAwait(false);

        await _endpoint.SendAsync(message, sendPipe, cancellationToken).ConfigureAwait(false);
    }

    public async Task SendAsync<T>(object values, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));
        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));

        (var message, IPipe<SendContext<T>> sendPipe) =
            await MessageInitializerCache<T>.InitializeMessageAsync(values, GetPipeProxy(pipe), cancellationToken).ConfigureAwait(false);

        await _endpoint.SendAsync(message, sendPipe, cancellationToken).ConfigureAwait(false);
    }

    public async Task SendAsync<T>(object values, IPipe<SendContext> pipe, CancellationToken cancellationToken)
        where T : class
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));
        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));

        (var message, IPipe<SendContext<T>> sendPipe) =
            await MessageInitializerCache<T>.InitializeMessageAsync(values, GetPipeProxy<T>(pipe), cancellationToken).ConfigureAwait(false);

        await _endpoint.SendAsync(message, sendPipe, cancellationToken).ConfigureAwait(false);
    }

    protected abstract IPipe<SendContext<T>> GetPipeProxy<T>(IPipe<SendContext<T>>? pipe = default)
        where T : class;
}
