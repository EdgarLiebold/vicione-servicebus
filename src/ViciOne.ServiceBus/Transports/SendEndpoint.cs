using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Initializers;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Provides a send endpoint implementation.
/// </summary>
public class SendEndpoint :
    ITransportSendEndpoint,
    IAsyncDisposable
{
    readonly ConnectHandle? _observerHandle = null!;
    readonly ISendPipe _sendPipe;
    readonly ISendTransport _transport;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="transport">The transport value.</param>
    /// <param name="context">The operation context.</param>
    /// <param name="destinationAddress">The destination address value.</param>
    /// <param name="sendPipe">The send pipe value.</param>
    /// <param name="observerHandle">The observer handle value.</param>
    public SendEndpoint(ISendTransport transport, ReceiveEndpointContext context, Uri destinationAddress, ISendPipe sendPipe,
        ConnectHandle? observerHandle = null)
    {
        _transport = transport;
        _sendPipe = sendPipe;
        _observerHandle = observerHandle;

        DestinationAddress = destinationAddress;
        SourceAddress = context.InputAddress;
        Serialization = context.Serialization;
        Serializer = context.Serialization.GetMessageSerializer();
    }

    Uri DestinationAddress { get; }
    Uri SourceAddress { get; }

    IMessageSerializer Serializer { get; }
    ISerialization Serialization { get; }

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public ValueTask DisposeAsync()
    {
        _observerHandle?.Disconnect();

        return _transport switch
        {
            IAsyncDisposable disposable => disposable.DisposeAsync(),
            _ => default
        };
    }

    /// <summary>
    /// Connects send observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        return _transport.ConnectSendObserver(observer);
    }

    /// <summary>
    /// Creates send context.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<SendContext<T>> CreateSendContextAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        return _transport.CreateSendContextAsync(message, new SendEndpointPipe<T>(this, pipe), cancellationToken);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync<T>(T message, CancellationToken cancellationToken)
        where T : class
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        return _transport.SendAsync(message, new SendEndpointPipe<T>(this), cancellationToken);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));

        return _transport.SendAsync(message, new SendEndpointPipe<T>(this, pipe), cancellationToken);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync(object message, CancellationToken cancellationToken)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var messageType = message.GetType();

        return SendEndpointConverterCache.SendAsync(this, message, messageType, cancellationToken);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="messageType">The message type value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync(object message, Type messageType, CancellationToken cancellationToken)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (messageType == null)
            throw new ArgumentNullException(nameof(messageType));

        return SendEndpointConverterCache.SendAsync(this, message, messageType, cancellationToken);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync<T>(T message, IPipe<SendContext> pipe, CancellationToken cancellationToken)
        where T : class
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));

        return _transport.SendAsync(message, new SendEndpointPipe<T>(this, pipe), cancellationToken);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync(object message, IPipe<SendContext> pipe, CancellationToken cancellationToken)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));

        var messageType = message.GetType();

        return SendEndpointConverterCache.SendAsync(this, message, messageType, pipe, cancellationToken);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="messageType">The message type value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="values">The values value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task SendAsync<T>(object values, CancellationToken cancellationToken)
        where T : class
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        (var message, IPipe<SendContext<T>> sendPipe) =
            await MessageInitializerCache<T>.InitializeMessageAsync(values, new SendEndpointPipe<T>(this), cancellationToken).ConfigureAwait(false);

        await _transport.SendAsync(message, sendPipe, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="values">The values value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task SendAsync<T>(object values, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        (var message, IPipe<SendContext<T>> sendPipe) =
            await MessageInitializerCache<T>.InitializeMessageAsync(values, new SendEndpointPipe<T>(this, pipe), cancellationToken).ConfigureAwait(false);

        await _transport.SendAsync(message, sendPipe, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="values">The values value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task SendAsync<T>(object values, IPipe<SendContext> pipe, CancellationToken cancellationToken)
        where T : class
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));
        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));

        (var message, IPipe<SendContext<T>> sendPipe) =
            await MessageInitializerCache<T>.InitializeMessageAsync(values, new SendEndpointPipe<T>(this, pipe), cancellationToken).ConfigureAwait(false);

        await _transport.SendAsync(message, sendPipe, cancellationToken).ConfigureAwait(false);
    }


    class SendEndpointPipe<T> :
        IPipe<SendContext<T>>
        where T : class
    {
        readonly SendEndpoint _endpoint;
        readonly IPipe<SendContext<T>>? _pipe;
        readonly ISendContextPipe? _sendContextPipe = null!;

        public SendEndpointPipe(SendEndpoint endpoint)
        {
            _endpoint = endpoint;
            _pipe = default;
            _sendContextPipe = default;
        }

        public SendEndpointPipe(SendEndpoint endpoint, IPipe<SendContext<T>> pipe)
        {
            _endpoint = endpoint;
            _pipe = pipe;
            _sendContextPipe = pipe as ISendContextPipe;
        }

        public void Probe(ProbeContext context)
        {
            _pipe?.Probe(context);
        }

        public async Task SendAsync(SendContext<T> context)
        {
            context.Serializer = _endpoint.Serializer;
            context.Serialization = _endpoint.Serialization;
            context.DestinationAddress = _endpoint.DestinationAddress;

            if (context.SourceAddress == null)
                context.SourceAddress = _endpoint.SourceAddress;

            if (_sendContextPipe != null)
                await _sendContextPipe.SendAsync(context).ConfigureAwait(false);

            if (_endpoint._sendPipe != null)
                await _endpoint._sendPipe.SendAsync(context).ConfigureAwait(false);

            if (_pipe != null && _pipe.IsNotEmpty())
                await _pipe.SendAsync(context).ConfigureAwait(false);

            context.ConversationId ??= NewId.NextGuid();
        }
    }
}
