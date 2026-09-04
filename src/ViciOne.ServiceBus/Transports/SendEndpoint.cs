using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Initializers;

namespace ViciOne.ServiceBus.Transports;

public class SendEndpoint :
    ITransportSendEndpoint,
    IAsyncDisposable
{
    readonly ConnectHandle? _observerHandle = null!;
    readonly ISendPipe _sendPipe;
    readonly ISendTransport _transport;

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

    public ValueTask DisposeAsync()
    {
        _observerHandle?.Disconnect();

        return _transport switch
        {
            IAsyncDisposable disposable => disposable.DisposeAsync(),
            _ => default
        };
    }

    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        return _transport.ConnectSendObserver(observer);
    }

    public Task<SendContext<T>> CreateSendContextAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        return _transport.CreateSendContextAsync(message, new SendEndpointPipe<T>(this, pipe), cancellationToken);
    }

    public Task SendAsync<T>(T message, CancellationToken cancellationToken)
        where T : class
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        return _transport.SendAsync(message, new SendEndpointPipe<T>(this), cancellationToken);
    }

    public Task SendAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));

        return _transport.SendAsync(message, new SendEndpointPipe<T>(this, pipe), cancellationToken);
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

    public Task SendAsync<T>(T message, IPipe<SendContext> pipe, CancellationToken cancellationToken)
        where T : class
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));

        return _transport.SendAsync(message, new SendEndpointPipe<T>(this, pipe), cancellationToken);
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
            await MessageInitializerCache<T>.InitializeMessageAsync(values, new SendEndpointPipe<T>(this), cancellationToken).ConfigureAwait(false);

        await _transport.SendAsync(message, sendPipe, cancellationToken).ConfigureAwait(false);
    }

    public async Task SendAsync<T>(object values, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        (var message, IPipe<SendContext<T>> sendPipe) =
            await MessageInitializerCache<T>.InitializeMessageAsync(values, new SendEndpointPipe<T>(this, pipe), cancellationToken).ConfigureAwait(false);

        await _transport.SendAsync(message, sendPipe, cancellationToken).ConfigureAwait(false);
    }

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
            // ReSharper disable once SuspiciousTypeConversion.Global
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
