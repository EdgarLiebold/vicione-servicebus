using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Initializers;

namespace ViciOne.ServiceBus.Context;

/// <summary>Adds a typed message view to an untyped consume context.</summary>
/// <typeparam name="TMessage">The consumed message contract.</typeparam>
public sealed class MessageConsumeContext<TMessage> :
    ConsumeContext<TMessage>,
    ConsumeContext
    where TMessage : class
{
    readonly ConsumeContext _context;

    /// <summary>Creates a typed consume-context view.</summary>
    /// <param name="context">The untyped consume context to wrap.</param>
    /// <param name="message">The typed message exposed by the view.</param>
    public MessageConsumeContext(ConsumeContext context, TMessage message)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));

        Message = message ?? throw new ArgumentNullException(nameof(message));
    }

    /// <inheritdoc />
    public TMessage Message { get; }

    /// <inheritdoc />
    public IOutgoingMessages Outgoing => _context.Outgoing;

    /// <inheritdoc />
    public Task NotifyConsumedAsync(TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
    {
        return _context.NotifyConsumedAsync(this, duration, consumerType, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public Task NotifyFaultedAsync(TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
    {
        return _context.NotifyFaultedAsync(this, duration, consumerType, exception, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public bool HasPayloadType(Type payloadType)
    {
        ArgumentNullException.ThrowIfNull(payloadType);
        return payloadType.IsInstanceOfType(this) || _context.HasPayloadType(payloadType);
    }

    /// <inheritdoc />
    public bool TryGetPayload<T>([NotNullWhen(true)] out T? payload)
        where T : class
    {
        if (this is T context)
        {
            payload = context;
            return true;
        }

        return _context.TryGetPayload(out payload);
    }

    /// <inheritdoc />
    public T GetOrAddPayload<T>(PayloadFactory<T> payloadFactory)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(payloadFactory);

        if (this is T context)
            return context;

        return _context.GetOrAddPayload(payloadFactory);
    }

    /// <inheritdoc />
    public T AddOrUpdatePayload<T>(PayloadFactory<T> addFactory, UpdatePayloadFactory<T> updateFactory)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(addFactory);
        ArgumentNullException.ThrowIfNull(updateFactory);

        if (this is T context)
            return context;

        return _context.AddOrUpdatePayload(addFactory, updateFactory);
    }

    /// <inheritdoc />
    public CancellationToken CancellationToken => _context.CancellationToken;

    /// <inheritdoc />
    public Guid? MessageId => _context.MessageId;

    /// <inheritdoc />
    public Guid? RequestId => _context.RequestId;

    /// <inheritdoc />
    public Guid? CorrelationId => _context.CorrelationId;

    /// <inheritdoc />
    public Guid? ConversationId => _context.ConversationId;

    /// <inheritdoc />
    public Guid? InitiatorId => _context.InitiatorId;

    /// <inheritdoc />
    public DateTimeOffset? ExpirationTime => _context.ExpirationTime;

    /// <inheritdoc />
    public Uri? SourceAddress => _context.SourceAddress;

    /// <inheritdoc />
    public Uri? DestinationAddress => _context.DestinationAddress;

    /// <inheritdoc />
    public Uri? ResponseAddress => _context.ResponseAddress;

    /// <inheritdoc />
    public Uri? FaultAddress => _context.FaultAddress;

    /// <inheritdoc />
    public DateTimeOffset? SentTime => _context.SentTime;

    /// <inheritdoc />
    public Headers Headers => _context.Headers;

    /// <inheritdoc />
    public HostInfo Host => _context.Host;

    /// <inheritdoc />
    public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _context.ConnectPublishObserver(observer);
    }

    /// <inheritdoc />
    public Task PublishAsync<T>(T message, CancellationToken cancellationToken)
        where T : class
    {
        return _context.PublishAsync(message, cancellationToken);
    }

    /// <inheritdoc />
    public Task PublishAsync<T>(T message, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken)
        where T : class
    {
        return _context.PublishAsync(message, publishPipe, cancellationToken);
    }

    /// <inheritdoc />
    public Task PublishAsync<T>(T message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken)
        where T : class
    {
        return _context.PublishAsync(message, publishPipe, cancellationToken);
    }

    /// <inheritdoc />
    public Task PublishAsync(object message, CancellationToken cancellationToken)
    {
        return _context.PublishAsync(message, cancellationToken);
    }

    /// <inheritdoc />
    public Task PublishAsync(object message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken)
    {
        return _context.PublishAsync(message, publishPipe, cancellationToken);
    }

    /// <inheritdoc />
    public Task PublishAsync(object message, Type messageType, CancellationToken cancellationToken)
    {
        return _context.PublishAsync(message, messageType, cancellationToken);
    }

    /// <inheritdoc />
    public Task PublishAsync(object message, Type messageType, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken)
    {
        return _context.PublishAsync(message, messageType, publishPipe, cancellationToken);
    }

    /// <inheritdoc />
    public Task PublishAsync<T>(object values, CancellationToken cancellationToken)
        where T : class
    {
        return _context.PublishAsync<T>(values, cancellationToken);
    }

    /// <inheritdoc />
    public Task PublishAsync<T>(object values, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken)
        where T : class
    {
        return _context.PublishAsync(values, publishPipe, cancellationToken);
    }

    /// <inheritdoc />
    public Task PublishAsync<T>(object values, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken)
        where T : class
    {
        return _context.PublishAsync<T>(values, publishPipe, cancellationToken);
    }

    /// <inheritdoc />
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _context.ConnectSendObserver(observer);
    }

    /// <inheritdoc />
    public Task<ISendEndpoint> GetSendEndpointAsync(Uri address, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(address);
        return _context.GetSendEndpointAsync(address, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public ReceiveContext ReceiveContext => _context.ReceiveContext;
    /// <inheritdoc />
    public SerializerContext SerializerContext => _context.SerializerContext;

    /// <inheritdoc />
    public Task ConsumeCompleted => _context.ConsumeCompleted;

    /// <inheritdoc />
    public IEnumerable<string> SupportedMessageTypes => _context.SupportedMessageTypes;

    /// <inheritdoc />
    public bool HasMessageType(Type messageType)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        return _context.HasMessageType(messageType) || messageType.IsInstanceOfType(Message);
    }

    /// <inheritdoc />
    public bool TryGetMessage<T>([NotNullWhen(true)] out ConsumeContext<T>? consumeContext)
        where T : class
    {
        if (_context.TryGetMessage(out consumeContext))
            return true;

        if (Message is T message)
        {
            consumeContext = this is ConsumeContext<T> currentContext
                ? currentContext
                : new MessageConsumeContext<T>(this, message);
            return true;
        }

        consumeContext = null;
        return false;
    }

    /// <inheritdoc />
    public void AddConsumeTask(Task task)
    {
        ArgumentNullException.ThrowIfNull(task);
        _context.AddConsumeTask(task);
    }

    /// <inheritdoc />
    public Task RespondAsync<T>(T message)
        where T : class
    {
        return _context.RespondAsync(message);
    }

    /// <inheritdoc />
    public Task RespondAsync<T>(T message, SendOptions options)
        where T : class
    {
        return _context.RespondAsync(message, options);
    }

    /// <inheritdoc />
    public Task RespondAsync<T>(T message, IPipe<SendContext<T>> sendPipe)
        where T : class
    {
        return _context.RespondAsync(message, sendPipe);
    }

    /// <inheritdoc />
    public Task RespondAsync<T>(T message, IPipe<SendContext> sendPipe)
        where T : class
    {
        return _context.RespondAsync(message, sendPipe);
    }

    /// <inheritdoc />
    public Task RespondAsync(object message)
    {
        return _context.RespondAsync(message);
    }

    /// <inheritdoc />
    public Task RespondAsync(object message, Type messageType)
    {
        return _context.RespondAsync(message, messageType);
    }

    /// <inheritdoc />
    public Task RespondAsync(object message, IPipe<SendContext> sendPipe)
    {
        return _context.RespondAsync(message, sendPipe);
    }

    /// <inheritdoc />
    public Task RespondAsync(object message, Type messageType, IPipe<SendContext> sendPipe)
    {
        return _context.RespondAsync(message, messageType, sendPipe);
    }

    /// <inheritdoc />
    public Task RespondAsync<T>(object values)
        where T : class
    {
        return RespondWithMessageAsync<T>(values);
    }

    /// <inheritdoc />
    public Task RespondAsync<T>(object values, IPipe<SendContext<T>> sendPipe)
        where T : class
    {
        return RespondWithMessageAsync(values, sendPipe);
    }

    /// <inheritdoc />
    public Task RespondAsync<T>(object values, IPipe<SendContext> sendPipe)
        where T : class
    {
        return RespondWithMessageAsync<T>(values, sendPipe);
    }

    /// <inheritdoc />
    public void DeferResponse<T>(T message)
        where T : class
    {
        _context.DeferResponse(message);
    }

    /// <inheritdoc />
    public Task NotifyConsumedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
        where T : class
    {
        return _context.NotifyConsumedAsync(context, duration, consumerType, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public Task NotifyFaultedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
        where T : class
    {
        return _context.NotifyFaultedAsync(context, duration, consumerType, exception, cancellationToken: cancellationToken);
    }

    /// <summary>Initializes a response contract from the supplied values and the consumed message, then sends it through the response endpoint.</summary>
    /// <typeparam name="T">The response contract.</typeparam>
    /// <param name="values">The values used to initialize the response.</param>
    /// <param name="responsePipe">An optional pipe that configures the response send context.</param>
    /// <returns>A task that completes when the response send completes.</returns>
    async Task RespondWithMessageAsync<T>(object values, IPipe<SendContext<T>>? responsePipe = default)
        where T : class
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        var responseEndpoint = await this.GetResponseEndpointAsync<T>(_context.CancellationToken).ConfigureAwait(false);

        (var message, IPipe<SendContext<T>> sendPipe) =
            await MessageInitializerCache<T>.InitializeMessageAsync(_context, values, new object[] { Message }, responsePipe).ConfigureAwait(false);

        await ConsumeTaskAsync(responseEndpoint.SendAsync(message, sendPipe, _context.CancellationToken)).ConfigureAwait(false);
    }

    Task ConsumeTaskAsync(Task task)
    {
        _context.AddConsumeTask(task);

        return task;
    }
}
