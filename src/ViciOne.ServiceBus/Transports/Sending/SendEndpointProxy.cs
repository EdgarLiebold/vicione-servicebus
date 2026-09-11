using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Internals.Dispatching;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Decorates a send endpoint and injects a pipe into every send-context path.</summary>
public abstract class SendEndpointProxy :
    ITransportSendEndpoint
{
    readonly ISendEndpoint _endpoint;
    readonly ITransportSendEndpoint? _transportEndpoint;

    /// <summary>Initializes the proxy with the endpoint to decorate.</summary>
    /// <param name="endpoint">The wrapped send endpoint.</param>
    protected SendEndpointProxy(ISendEndpoint endpoint)
    {
        _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        _transportEndpoint = endpoint as ITransportSendEndpoint;
    }

    /// <summary>Gets the destination endpoint decorated by this proxy.</summary>
    public ISendEndpoint Endpoint => _endpoint;

    /// <inheritdoc />
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _endpoint.ConnectSendObserver(observer);
    }

    /// <inheritdoc />
    public Task<SendContext<T>> CreateSendContextAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);
        cancellationToken.ThrowIfCancellationRequested();

        IPipe<SendContext<T>> proxy = GetPipeProxy(pipe)
            ?? throw new InvalidOperationException("The send endpoint proxy returned no typed send pipe.");
        return _transportEndpoint?.CreateSendContextAsync(message, proxy, cancellationToken)
            ?? throw new InvalidOperationException("The wrapped endpoint does not support transport send-context creation.");
    }

    /// <inheritdoc />
    public virtual Task SendAsync<T>(T message, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        cancellationToken.ThrowIfCancellationRequested();

        IPipe<SendContext<T>> proxy = GetPipeProxy<T>()
            ?? throw new InvalidOperationException("The send endpoint proxy returned no typed send pipe.");
        return _endpoint.SendAsync(message, proxy, cancellationToken);
    }

    /// <inheritdoc />
    public virtual Task SendAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);
        cancellationToken.ThrowIfCancellationRequested();

        IPipe<SendContext<T>> proxy = GetPipeProxy(pipe)
            ?? throw new InvalidOperationException("The send endpoint proxy returned no typed send pipe.");
        return _endpoint.SendAsync(message, proxy, cancellationToken);
    }

    /// <inheritdoc />
    public virtual Task SendAsync<T>(T message, IPipe<SendContext> pipe, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);
        cancellationToken.ThrowIfCancellationRequested();

        IPipe<SendContext<T>> proxy = GetPipeProxy<T>(pipe)
            ?? throw new InvalidOperationException("The send endpoint proxy returned no typed send pipe.");
        return _endpoint.SendAsync(message, proxy, cancellationToken);
    }

    /// <inheritdoc />
    public Task SendAsync(object message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        cancellationToken.ThrowIfCancellationRequested();

        var messageType = message.GetType();

        return SendEndpointDispatcher.SendAsync(this, message, messageType, cancellationToken);
    }

    /// <inheritdoc />
    public Task SendAsync(object message, Type messageType, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);
        cancellationToken.ThrowIfCancellationRequested();

        return SendEndpointDispatcher.SendAsync(this, message, messageType, cancellationToken);
    }

    /// <inheritdoc />
    public Task SendAsync(object message, IPipe<SendContext> pipe, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);
        cancellationToken.ThrowIfCancellationRequested();

        var messageType = message.GetType();

        return SendEndpointDispatcher.SendAsync(this, message, messageType, pipe, cancellationToken);
    }

    /// <inheritdoc />
    public Task SendAsync(object message, Type messageType, IPipe<SendContext> pipe, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);
        ArgumentNullException.ThrowIfNull(pipe);
        cancellationToken.ThrowIfCancellationRequested();

        return SendEndpointDispatcher.SendAsync(this, message, messageType, pipe, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SendAsync<T>(object values, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(values);
        cancellationToken.ThrowIfCancellationRequested();

        (var message, IPipe<SendContext<T>> sendPipe) =
            await MessageInitializerCache<T>.InitializeMessageAsync(values, GetPipeProxy<T>()
                ?? throw new InvalidOperationException("The send endpoint proxy returned no typed send pipe."), cancellationToken).ConfigureAwait(false);

        await _endpoint.SendAsync(message, sendPipe, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SendAsync<T>(object values, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(pipe);
        cancellationToken.ThrowIfCancellationRequested();

        (var message, IPipe<SendContext<T>> sendPipe) =
            await MessageInitializerCache<T>.InitializeMessageAsync(values, GetPipeProxy(pipe)
                ?? throw new InvalidOperationException("The send endpoint proxy returned no typed send pipe."), cancellationToken).ConfigureAwait(false);

        await _endpoint.SendAsync(message, sendPipe, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SendAsync<T>(object values, IPipe<SendContext> pipe, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(pipe);
        cancellationToken.ThrowIfCancellationRequested();

        (var message, IPipe<SendContext<T>> sendPipe) =
            await MessageInitializerCache<T>.InitializeMessageAsync(values, GetPipeProxy<T>(pipe)
                ?? throw new InvalidOperationException("The send endpoint proxy returned no typed send pipe."), cancellationToken).ConfigureAwait(false);

        await _endpoint.SendAsync(message, sendPipe, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Creates the typed send pipe that applies proxy-specific metadata.</summary>
    /// <typeparam name="T">The outgoing message contract.</typeparam>
    /// <param name="pipe">The optional caller-supplied typed send pipe.</param>
    /// <returns>The proxy pipe passed to the wrapped endpoint.</returns>
    protected abstract IPipe<SendContext<T>> GetPipeProxy<T>(IPipe<SendContext<T>>? pipe = default)
        where T : class;
}
