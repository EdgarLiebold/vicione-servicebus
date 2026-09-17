using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Internals.Dispatching;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Mediator.Contexts;

/// <summary>Materializes and dispatches messages through the mediator receive pipeline.</summary>
internal sealed class MediatorSendEndpoint :
    ITransportSendEndpoint,
    IPublishEndpointProvider,
    ISendEndpointProvider,
    IMessageRouteProvider
{
    readonly Uri _destinationAddress;
    readonly IReceivePipeDispatcher _dispatcher;
    readonly ILogContext? _logContext;
    readonly IObjectDeserializer _objectDeserializer;
    readonly JsonSerializerOptions _bodySerializerOptions;
    readonly MessageLimits _messageLimits;
    readonly PublishObservable _publishObservers;
    readonly MediatorPublishSendEndpoint _publishSendEndpoint;
    readonly IPublishTopologyConfigurator _publishTopology;
    readonly ReceiveObservable _receiveObservers;
    readonly SendObservable _sendObservers;
    readonly ISendPipe _sendPipe;
    readonly Uri? _sourceAddress;
    readonly MediatorSendEndpoint? _sourceEndpoint;

    MediatorSendEndpoint(
        IReceiveEndpointConfiguration configuration,
        IReceivePipeDispatcher dispatcher,
        ILogContext? logContext,
        SendObservable sendObservers,
        PublishObservable publishObservers,
        MessageLimits messageLimits)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _logContext = logContext;
        _sendObservers = sendObservers ?? throw new ArgumentNullException(nameof(sendObservers));
        _publishObservers = publishObservers ?? throw new ArgumentNullException(nameof(publishObservers));

        _destinationAddress = configuration.InputAddress;
        _publishTopology = configuration.Topology.Publish;
        _receiveObservers = configuration.ReceiveObservers;

        _objectDeserializer = ServiceBusMetadataJson.ObjectDeserializer;
        _messageLimits = messageLimits ?? throw new ArgumentNullException(nameof(messageLimits));
        _bodySerializerOptions = new JsonSerializerOptions(ServiceBusMetadataJson.Options)
        {
            MaxDepth = messageLimits.MaxJsonDepth,
        };

        _sendPipe = configuration.Send.CreatePipe();
        _publishSendEndpoint = new MediatorPublishSendEndpoint(this, configuration.Publish.CreatePipe(), publishObservers);
    }

    /// <summary>Initializes the primary mediator endpoint and its request-response source endpoint.</summary>
    /// <param name="configuration">The primary mediator endpoint configuration.</param>
    /// <param name="dispatcher">The primary receive dispatcher.</param>
    /// <param name="logContext">The log context inherited by dispatch operations.</param>
    /// <param name="sendObservers">The observers notified around every send.</param>
    /// <param name="publishObservers">The observers notified around mediator publications.</param>
    /// <param name="sourceConfiguration">The response endpoint configuration.</param>
    /// <param name="sourceDispatcher">The response receive dispatcher.</param>
    /// <param name="messageLimits">The message limits enforced before dispatch.</param>
    public MediatorSendEndpoint(IReceiveEndpointConfiguration configuration, IReceivePipeDispatcher dispatcher, ILogContext? logContext,
        SendObservable sendObservers, PublishObservable publishObservers, IReceiveEndpointConfiguration sourceConfiguration,
        IReceivePipeDispatcher sourceDispatcher, MessageLimits messageLimits)
        : this(configuration, dispatcher, logContext, sendObservers, publishObservers, messageLimits)
    {
        ArgumentNullException.ThrowIfNull(sourceConfiguration);
        ArgumentNullException.ThrowIfNull(sourceDispatcher);
        _sourceAddress = sourceConfiguration.InputAddress;
        _sourceEndpoint = new MediatorSendEndpoint(
            sourceConfiguration,
            sourceDispatcher,
            logContext,
            sendObservers,
            publishObservers,
            messageLimits);
    }

    /// <summary>Registers an observer for mediator publications.</summary>
    /// <param name="observer">The observer notified around each publication.</param>
    /// <returns>A handle that disconnects the observer.</returns>
    public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
    {
        return _publishSendEndpoint.ConnectPublishObserver(observer);
    }

    IMessageRouteTable IMessageRouteProvider.MessageRoutes => MessageRouteTable.Empty;

    /// <inheritdoc />
    public Task<ISendEndpoint> GetPublishSendEndpointAsync<T>(CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<ISendEndpoint>(cancellationToken);

        return Task.FromResult<ISendEndpoint>(_publishSendEndpoint);
    }

    /// <inheritdoc />
    public Task<ISendEndpoint> GetSendEndpointAsync(Uri address, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(address);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<ISendEndpoint>(cancellationToken);

        return Task.FromResult(GetSendEndpoint(address));
    }

    internal ISendEndpoint GetSendEndpoint(Uri address)
    {
        ArgumentNullException.ThrowIfNull(address);

        if (address.Equals(_destinationAddress))
            return this;
        if (_sourceEndpoint is not null && address.Equals(_sourceAddress))
            return _sourceEndpoint;

        return new AddressedMediatorSendEndpoint(this, address);
    }

    /// <summary>Registers an observer for mediator sends that are not publications.</summary>
    /// <param name="observer">The observer notified around each send.</param>
    /// <returns>A handle that disconnects the observer.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _sendObservers.Connect(observer);
    }

    /// <inheritdoc />
    public Task SendAsync<T>(T message, CancellationToken cancellationToken)
        where T : class
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        cancellationToken.ThrowIfCancellationRequested();

        return SendMessageAsync(message, new MediatorPipe<T>(this), cancellationToken);
    }

    /// <inheritdoc />
    public Task SendAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));
        cancellationToken.ThrowIfCancellationRequested();

        return SendMessageAsync(message, new MediatorPipe<T>(this, pipe), cancellationToken);
    }

    /// <inheritdoc />
    public Task SendAsync(object message, CancellationToken cancellationToken)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        cancellationToken.ThrowIfCancellationRequested();

        var messageType = message.GetType();

        return SendEndpointDispatcher.SendAsync(this, message, messageType, cancellationToken);
    }

    /// <inheritdoc />
    public Task SendAsync(object message, Type messageType, CancellationToken cancellationToken)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (messageType == null)
            throw new ArgumentNullException(nameof(messageType));
        cancellationToken.ThrowIfCancellationRequested();

        return SendEndpointDispatcher.SendAsync(this, message, messageType, cancellationToken);
    }

    /// <inheritdoc />
    public Task SendAsync<T>(T message, IPipe<SendContext> pipe, CancellationToken cancellationToken)
        where T : class
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));
        cancellationToken.ThrowIfCancellationRequested();

        return SendMessageAsync(message, new MediatorPipe<T>(this, pipe), cancellationToken);
    }

    /// <inheritdoc />
    public Task SendAsync(object message, IPipe<SendContext> pipe, CancellationToken cancellationToken)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));
        cancellationToken.ThrowIfCancellationRequested();

        var messageType = message.GetType();

        return SendEndpointDispatcher.SendAsync(this, message, messageType, pipe, cancellationToken);
    }

    /// <inheritdoc />
    public Task SendAsync(object message, Type messageType, IPipe<SendContext> pipe, CancellationToken cancellationToken)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (messageType == null)
            throw new ArgumentNullException(nameof(messageType));
        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));
        cancellationToken.ThrowIfCancellationRequested();

        return SendEndpointDispatcher.SendAsync(this, message, messageType, pipe, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SendAsync<T>(object values, CancellationToken cancellationToken)
        where T : class
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));
        cancellationToken.ThrowIfCancellationRequested();

        (var message, IPipe<SendContext<T>> sendPipe) =
            await MessageInitializerCache<T>.InitializeMessageAsync(values, new MediatorPipe<T>(this), cancellationToken).ConfigureAwait(false);

        await SendMessageAsync(message, sendPipe, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SendAsync<T>(object values, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));
        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));
        cancellationToken.ThrowIfCancellationRequested();

        (var message, IPipe<SendContext<T>> sendPipe) =
            await MessageInitializerCache<T>.InitializeMessageAsync(values, new MediatorPipe<T>(this, pipe), cancellationToken).ConfigureAwait(false);

        await SendMessageAsync(message, sendPipe, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SendAsync<T>(object values, IPipe<SendContext> pipe, CancellationToken cancellationToken)
        where T : class
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));
        if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));
        cancellationToken.ThrowIfCancellationRequested();

        (var message, IPipe<SendContext<T>> sendPipe) =
            await MessageInitializerCache<T>.InitializeMessageAsync(values, new MediatorPipe<T>(this, pipe), cancellationToken).ConfigureAwait(false);

        await SendMessageAsync(message, sendPipe, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<SendContext<T>> CreateSendContextAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);
        cancellationToken.ThrowIfCancellationRequested();
        LogContext.SetCurrentIfNull(_logContext);

        var context = new MessageSendContext<T>(message, cancellationToken);

        Task configuration = pipe.SendAsync(context)
            ?? throw new InvalidOperationException("The send-context pipe returned no configuration task.");
        await configuration.ConfigureAwait(false);

        return context;
    }

    async Task SendMessageAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        LogContext.SetCurrentIfNull(_logContext);

        var context = new MessageSendContext<T>(message, cancellationToken);

        Task configuration = pipe.SendAsync(context)
            ?? throw new InvalidOperationException("The mediator send pipe returned no configuration task.");
        await configuration.ConfigureAwait(false);

        if (ForwardingExpiration.TryDiscard(context))
            return;

        bool isPublish = context.IsPublish;
        ISendObserver observers = isPublish ? _publishObservers : _sendObservers;

        try
        {
            if ((isPublish ? _publishObservers.Count : _sendObservers.Count) > 0)
                await observers.PreSendAsync(context).ConfigureAwait(false);

            MessageBody messageBody = await MediatorMessageBodySerializer.SerializeAsync(
                context.Message,
                _bodySerializerOptions,
                _messageLimits,
                context.DestinationAddress ?? _destinationAddress,
                cancellationToken).ConfigureAwait(false);

            var receiveContext = new MediatorReceiveContext<T>(
                context,
                this,
                this,
                _publishTopology,
                _receiveObservers,
                _objectDeserializer,
                messageBody)
            {
                IsDelivered = context.IsPublish && !context.Mandatory
            };

            Task dispatch = _dispatcher.DispatchAsync(receiveContext, NoLockReceiveContext.Instance, cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("The mediator receive dispatcher returned no dispatch task.");
            await dispatch.ConfigureAwait(false);

            if ((isPublish ? _publishObservers.Count : _sendObservers.Count) > 0)
                await observers.PostSendAsync(context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if ((isPublish ? _publishObservers.Count : _sendObservers.Count) > 0)
                await NotifyObserverFaultAsync(observers, context, ex, isPublish ? "publish" : "send").ConfigureAwait(false);

            throw;
        }
    }

    static async Task NotifyObserverFaultAsync<T>(
        ISendObserver observer,
        SendContext<T> context,
        Exception exception,
        string observerKind)
        where T : class
    {
        try
        {
            await observer.SendFaultAsync(context, exception).ConfigureAwait(false);
        }
        catch (Exception observerException)
        {
            LogContext.Error?.Log(observerException,
                "A mediator {ObserverKind}-fault observer failed after dispatch faulted: {DestinationAddress}",
                observerKind,
                context.DestinationAddress);
        }
    }

    sealed class MediatorPipe<TMessage> :
        IPipe<SendContext<TMessage>>
        where TMessage : class
    {
        readonly MediatorSendEndpoint _endpoint;
        readonly IPipe<SendContext<TMessage>>? _pipe;

        public MediatorPipe(MediatorSendEndpoint endpoint)
        {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            _pipe = default;
        }

        public MediatorPipe(MediatorSendEndpoint endpoint, IPipe<SendContext<TMessage>> pipe)
        {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            _pipe = pipe ?? throw new ArgumentNullException(nameof(pipe));
        }

        void IProbeSite.Probe(ProbeContext context)
        {
            _pipe?.Probe(context);
        }

        public async Task SendAsync(SendContext<TMessage> context)
        {
            ArgumentNullException.ThrowIfNull(context);
            context.CancellationToken.ThrowIfCancellationRequested();
            context.DestinationAddress = _endpoint._destinationAddress;

            context.SourceAddress ??= _endpoint._sourceAddress;

            if (_pipe is ISendContextPipe sendContextPipe)
            {
                Task generalConfiguration = sendContextPipe.SendAsync(context, context.CancellationToken)
                    ?? throw new InvalidOperationException("The general send-context pipe returned no configuration task.");
                await generalConfiguration.ConfigureAwait(false);
            }

            Task endpointConfiguration = _endpoint._sendPipe.SendAsync(context, context.CancellationToken)
                ?? throw new InvalidOperationException("The mediator endpoint send pipe returned no configuration task.");
            await endpointConfiguration.ConfigureAwait(false);

            if (_pipe is { } pipe && pipe.IsNotEmpty())
            {
                Task additionalConfiguration = pipe.SendAsync(context)
                    ?? throw new InvalidOperationException("The additional mediator send pipe returned no configuration task.");
                await additionalConfiguration.ConfigureAwait(false);
            }

            context.ConversationId ??= NewId.NextGuid();
        }
    }
}
