using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Mediator.Contexts;

/// <summary>
/// Provides a mediator send endpoint implementation.
/// </summary>
public class MediatorSendEndpoint :
    ITransportSendEndpoint,
    IPublishEndpointProvider,
    ISendEndpointProvider,
    IMessageRouteProvider
{
    readonly Uri _destinationAddress;
    readonly IReceivePipeDispatcher _dispatcher;
    readonly ILogContext? _logContext = null!;
    readonly IObjectDeserializer _objectDeserializer;
    readonly JsonSerializerOptions _bodySerializerOptions;
    readonly MessageLimits _messageLimits;
    readonly MediatorPublishSendEndpoint _publishSendEndpoint;
    readonly IPublishTopologyConfigurator _publishTopology;
    readonly ReceiveObservable _receiveObservers;
    readonly SendObservable _sendObservers;
    readonly ISendPipe _sendPipe;
    readonly Uri _sourceAddress = null!;
    readonly MediatorSendEndpoint _sourceEndpoint = null!;

    MediatorSendEndpoint(
        IReceiveEndpointConfiguration configuration,
        IReceivePipeDispatcher dispatcher,
        ILogContext? logContext,
        SendObservable sendObservers,
        MessageLimits messageLimits)
    {
        _dispatcher = dispatcher;
        _logContext = logContext;
        _sendObservers = sendObservers;

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
        _publishSendEndpoint = new MediatorPublishSendEndpoint(this, configuration.Publish.CreatePipe());
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="configuration">The configuration callback.</param>
    /// <param name="dispatcher">The dispatcher value.</param>
    /// <param name="logContext">The log context value.</param>
    /// <param name="sendObservers">The send observers value.</param>
    /// <param name="sourceConfiguration">The source configuration value.</param>
    /// <param name="sourceDispatcher">The source dispatcher value.</param>
    /// <param name="messageLimits">The mandatory mediator message limits.</param>
    public MediatorSendEndpoint(IReceiveEndpointConfiguration configuration, IReceivePipeDispatcher dispatcher, ILogContext? logContext,
        SendObservable sendObservers, IReceiveEndpointConfiguration sourceConfiguration, IReceivePipeDispatcher sourceDispatcher,
        MessageLimits messageLimits)
        : this(configuration, dispatcher, logContext, sendObservers, messageLimits)
    {
        _sourceAddress = sourceConfiguration.InputAddress;
        _sourceEndpoint = new MediatorSendEndpoint(sourceConfiguration, sourceDispatcher, logContext, sendObservers, messageLimits);
    }

    /// <summary>
    /// Connects publish observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
    {
        return _publishSendEndpoint.ConnectPublishObserver(observer);
    }

    IMessageRouteTable IMessageRouteProvider.MessageRoutes => MessageRouteTable.Empty;

    /// <summary>
    /// Gets publish send endpoint.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<ISendEndpoint> GetPublishSendEndpointAsync<T>(CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.ISendEndpoint>(cancellationToken); return Task.FromResult<ISendEndpoint>(_publishSendEndpoint);
    }

    /// <summary>
    /// Gets send endpoint.
    /// </summary>
    /// <param name="address">The address value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<ISendEndpoint> GetSendEndpointAsync(Uri address, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.ISendEndpoint>(cancellationToken); return Task.FromResult<ISendEndpoint>(address.Equals(_sourceAddress) ? _sourceEndpoint : this);
    }

    /// <summary>
    /// Connects send observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        return _sendObservers.Connect(observer);
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

        return SendMessageAsync(message, new MediatorPipe<T>(this), cancellationToken);
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

        return SendMessageAsync(message, new MediatorPipe<T>(this, pipe), cancellationToken);
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

        return SendMessageAsync(message, new MediatorPipe<T>(this, pipe), cancellationToken);
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
            await MessageInitializerCache<T>.InitializeMessageAsync(values, new MediatorPipe<T>(this), cancellationToken).ConfigureAwait(false);

        await SendMessageAsync(message, sendPipe, cancellationToken).ConfigureAwait(false);
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
            await MessageInitializerCache<T>.InitializeMessageAsync(values, new MediatorPipe<T>(this, pipe), cancellationToken).ConfigureAwait(false);

        await SendMessageAsync(message, sendPipe, cancellationToken).ConfigureAwait(false);
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
            await MessageInitializerCache<T>.InitializeMessageAsync(values, new MediatorPipe<T>(this, pipe), cancellationToken).ConfigureAwait(false);

        await SendMessageAsync(message, sendPipe, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates send context.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<SendContext<T>> CreateSendContextAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        LogContext.SetCurrentIfNull(_logContext);

        var context = new MessageSendContext<T>(message, cancellationToken);

        await pipe.SendAsync(context).ConfigureAwait(false);

        return context;
    }

    async Task SendMessageAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        LogContext.SetCurrentIfNull(_logContext);

        var context = new MessageSendContext<T>(message, cancellationToken);

        await pipe.SendAsync(context).ConfigureAwait(false);

        if (ForwardingExpiration.TryDiscard(context))
            return;

        long serializedBodyBytes = await MediatorMessageBodySizer.MeasureAsync(
            context.Message,
            _bodySerializerOptions,
            _messageLimits,
            _destinationAddress,
            cancellationToken).ConfigureAwait(false);

        var receiveContext = new MediatorReceiveContext<T>(
            context,
            this,
            this,
            _publishTopology,
            _receiveObservers,
            _objectDeserializer,
            serializedBodyBytes)
        {
            IsDelivered = context.IsPublish && !context.Mandatory
        };

        try
        {
            if (_sendObservers.Count > 0)
                await _sendObservers.PreSendAsync(context).ConfigureAwait(false);

            await _dispatcher.DispatchAsync(receiveContext, NoLockReceiveContext.Instance, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (_sendObservers.Count > 0)
                await _sendObservers.PostSendAsync(context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (_sendObservers.Count > 0)
                await _sendObservers.SendFaultAsync(context, ex).ConfigureAwait(false);

            throw;
        }
    }


    class MediatorPipe<TMessage> :
        IPipe<SendContext<TMessage>>
        where TMessage : class
    {
        readonly MediatorSendEndpoint _endpoint;
        readonly IPipe<SendContext<TMessage>>? _pipe;

        public MediatorPipe(MediatorSendEndpoint endpoint)
        {
            _endpoint = endpoint;
            _pipe = default;
        }

        public MediatorPipe(MediatorSendEndpoint endpoint, IPipe<SendContext<TMessage>> pipe)
        {
            _endpoint = endpoint;
            _pipe = pipe;
        }

        void IProbeSite.Probe(ProbeContext context)
        {
            _pipe?.Probe(context);
        }

        public async Task SendAsync(SendContext<TMessage> context)
        {
            context.DestinationAddress = _endpoint._destinationAddress;

            context.SourceAddress ??= _endpoint._sourceAddress;

            if (_pipe is ISendContextPipe sendContextPipe)
                await sendContextPipe.SendAsync(context).ConfigureAwait(false);

            await _endpoint._sendPipe.SendAsync(context).ConfigureAwait(false);

            if (_pipe is { } pipe && pipe.IsNotEmpty())
                await pipe.SendAsync(context).ConfigureAwait(false);

            context.ConversationId ??= NewId.NextGuid();
        }
    }
}
