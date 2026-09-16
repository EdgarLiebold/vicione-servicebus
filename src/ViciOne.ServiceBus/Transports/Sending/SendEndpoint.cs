using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Internals.Dispatching;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Applies endpoint serialization and addressing before dispatching through a send transport.</summary>
/// <remarks>
/// Required arguments are validated before cancellation. Started transport operations and endpoint configuration
/// stages retain their original outcomes; caller cancellation does not detach them or settle caller-owned input values.
/// </remarks>
internal sealed class SendEndpoint :
    ITransportSendEndpoint,
    IAsyncDisposable
{
    readonly object _disposeLock = new();
    readonly ConnectHandle? _observerHandle;
    readonly ISendPipe _sendPipe;
    readonly ISendTransport _transport;
    Task? _disposeTask;

    /// <summary>Initializes an addressed endpoint over an owned send transport.</summary>
    /// <param name="transport">The transport that performs physical sends.</param>
    /// <param name="context">The receive endpoint context that supplies source address and serialization.</param>
    /// <param name="destinationAddress">The normalized destination address.</param>
    /// <param name="sendPipe">The endpoint-level pipeline applied to every send.</param>
    /// <param name="observerHandle">The optional transport-observer connection owned by this endpoint.</param>
    internal SendEndpoint(ISendTransport transport, ReceiveEndpointContext context, Uri destinationAddress, ISendPipe sendPipe,
        ConnectHandle? observerHandle = null)
    {
        ArgumentNullException.ThrowIfNull(context);

        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _sendPipe = sendPipe ?? throw new ArgumentNullException(nameof(sendPipe));
        _observerHandle = observerHandle;

        DestinationAddress = destinationAddress ?? throw new ArgumentNullException(nameof(destinationAddress));
        SourceAddress = context.InputAddress
            ?? throw new InvalidOperationException("The receive endpoint context returned no source address.");
        Serialization = context.Serialization
            ?? throw new InvalidOperationException("The receive endpoint context returned no serialization registry.");
        Serializer = Serialization.GetMessageSerializer()
            ?? throw new InvalidOperationException("The serialization registry returned no message serializer.");
    }

    Uri DestinationAddress { get; }
    Uri SourceAddress { get; }

    IMessageSerializer Serializer { get; }
    ISerialization Serialization { get; }

    /// <summary>Disconnects endpoint observers and releases the owned send transport.</summary>
    /// <returns>A value task that completes after asynchronous transport disposal when supported.</returns>
    public ValueTask DisposeAsync()
    {
        lock (_disposeLock)
            return new ValueTask(_disposeTask ??= DisposeCoreAsync());
    }

    async Task DisposeCoreAsync()
    {
        IReadOnlyList<Exception> failures = await SendEndpointResourceRelease
            .CollectFailuresAsync(_observerHandle, _transport)
            .ConfigureAwait(false);

        if (failures.Count == 1)
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1)
            throw new AggregateException("The send endpoint observer and transport both failed during release.", failures);
    }

    /// <inheritdoc />
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _transport.ConnectSendObserver(observer)
            ?? throw new InvalidOperationException("The send transport returned no observer connection handle.");
    }

    /// <inheritdoc />
    public Task<SendContext<T>> CreateSendContextAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);
        cancellationToken.ThrowIfCancellationRequested();
        Task<SendContext<T>> creation = _transport.CreateSendContextAsync(message, new SendEndpointPipe<T>(this, pipe), cancellationToken)
            ?? throw new InvalidOperationException("The send transport returned no context-creation task.");

        if (creation.IsCompleted)
        {
            if (creation.IsCompletedSuccessfully && creation.Result is null)
                throw new InvalidOperationException("The send transport created no send context.");

            return creation;
        }

        return AwaitCreatedContextAsync(creation);
    }

    static async Task<SendContext<T>> AwaitCreatedContextAsync<T>(Task<SendContext<T>> creation)
        where T : class
    {
        return await creation.ConfigureAwait(false)
            ?? throw new InvalidOperationException("The send transport created no send context.");
    }

    /// <inheritdoc />
    public Task SendAsync<T>(T message, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        cancellationToken.ThrowIfCancellationRequested();

        return _transport.SendAsync(message, new SendEndpointPipe<T>(this), cancellationToken)
            ?? throw new InvalidOperationException("The send transport returned no send task.");
    }

    /// <inheritdoc />
    public Task SendAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);
        cancellationToken.ThrowIfCancellationRequested();

        return _transport.SendAsync(message, new SendEndpointPipe<T>(this, pipe), cancellationToken)
            ?? throw new InvalidOperationException("The send transport returned no send task.");
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
    public Task SendAsync<T>(T message, IPipe<SendContext> pipe, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);
        cancellationToken.ThrowIfCancellationRequested();

        return _transport.SendAsync(message, new SendEndpointPipe<T>(this, pipe), cancellationToken)
            ?? throw new InvalidOperationException("The send transport returned no send task.");
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
    public Task SendAsync<T>(object values, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(values);
        cancellationToken.ThrowIfCancellationRequested();

        return SendInitializedAsync(values, new SendEndpointPipe<T>(this), cancellationToken);
    }

    /// <inheritdoc />
    public Task SendAsync<T>(object values, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(pipe);
        cancellationToken.ThrowIfCancellationRequested();

        return SendInitializedAsync(values, new SendEndpointPipe<T>(this, pipe), cancellationToken);
    }

    /// <inheritdoc />
    public Task SendAsync<T>(object values, IPipe<SendContext> pipe, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(pipe);
        cancellationToken.ThrowIfCancellationRequested();

        return SendInitializedAsync(values, new SendEndpointPipe<T>(this, pipe), cancellationToken);
    }

    async Task SendInitializedAsync<T>(object values, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        (var message, IPipe<SendContext<T>> sendPipe) =
            await MessageInitializerCache<T>.InitializeMessageAsync(values, pipe, cancellationToken).ConfigureAwait(false);

        Task sendTask = _transport.SendAsync(message, sendPipe, cancellationToken)
            ?? throw new InvalidOperationException("The send transport returned no send task.");
        await sendTask.ConfigureAwait(false);
    }

    /// <summary>Projects endpoint metadata, configures general and typed stages, and preserves conversation identity.</summary>
    sealed class SendEndpointPipe<T> :
        IPipe<SendContext<T>>
        where T : class
    {
        readonly SendEndpoint _endpoint;
        readonly IPipe<SendContext<T>>? _pipe;
        readonly ISendContextPipe? _sendContextPipe;

        public SendEndpointPipe(SendEndpoint endpoint)
        {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            _pipe = default;
            _sendContextPipe = default;
        }

        public SendEndpointPipe(SendEndpoint endpoint, IPipe<SendContext<T>> pipe)
        {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            _pipe = pipe ?? throw new ArgumentNullException(nameof(pipe));
            _sendContextPipe = pipe as ISendContextPipe;
        }

        public void Probe(ProbeContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            _pipe?.Probe(context);
        }

        public async Task SendAsync(SendContext<T> context)
        {
            ArgumentNullException.ThrowIfNull(context);

            context.Serializer = _endpoint.Serializer;
            context.Serialization = _endpoint.Serialization;
            context.DestinationAddress = _endpoint.DestinationAddress;

            if (context.SourceAddress == null)
                context.SourceAddress = _endpoint.SourceAddress;

            if (_sendContextPipe != null)
            {
                Task generalConfiguration = _sendContextPipe.SendAsync(context, context.CancellationToken)
                    ?? throw new InvalidOperationException("The general send-context pipe returned no configuration task.");
                await generalConfiguration.ConfigureAwait(false);
            }

            Task endpointConfiguration = _endpoint._sendPipe.SendAsync(context, context.CancellationToken)
                ?? throw new InvalidOperationException("The endpoint send pipe returned no configuration task.");
            await endpointConfiguration.ConfigureAwait(false);

            if (_pipe != null && _pipe.IsNotEmpty())
            {
                Task additionalConfiguration = _pipe.SendAsync(context)
                    ?? throw new InvalidOperationException("The additional send pipe returned no configuration task.");
                await additionalConfiguration.ConfigureAwait(false);
            }

            context.ConversationId ??= NewId.NextGuid();
        }
    }
}
