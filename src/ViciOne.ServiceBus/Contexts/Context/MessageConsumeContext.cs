using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Initializers;

namespace ViciOne.ServiceBus.Context;

public class MessageConsumeContext<TMessage> :
    ConsumeContext<TMessage>,
    ConsumeContext
    where TMessage : class
{
    readonly ConsumeContext _context;

    public MessageConsumeContext(ConsumeContext context, TMessage message)
    {
        _context = context;

        Message = message;
    }

    public TMessage Message { get; }

    public Task NotifyConsumedAsync(TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
    {
        return _context.NotifyConsumedAsync(this, duration, consumerType, cancellationToken: cancellationToken);
    }

    public Task NotifyFaultedAsync(TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
    {
        return _context.NotifyFaultedAsync(this, duration, consumerType, exception, cancellationToken: cancellationToken);
    }

    public bool HasPayloadType(Type payloadType)
    {
        return payloadType.IsInstanceOfType(this) || _context.HasPayloadType(payloadType);
    }

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

    public T GetOrAddPayload<T>(PayloadFactory<T> payloadFactory)
        where T : class
    {
        if (this is T context)
            return context;

        return _context.GetOrAddPayload(payloadFactory);
    }

    public T AddOrUpdatePayload<T>(PayloadFactory<T> addFactory, UpdatePayloadFactory<T> updateFactory)
        where T : class
    {
        if (this is T context)
            return context;

        return _context.AddOrUpdatePayload(addFactory, updateFactory);
    }

    public CancellationToken CancellationToken => _context.CancellationToken;

    public Guid? MessageId => _context.MessageId;

    public Guid? RequestId => _context.RequestId;

    public Guid? CorrelationId => _context.CorrelationId;

    public Guid? ConversationId => _context.ConversationId;

    public Guid? InitiatorId => _context.InitiatorId;

    public DateTimeOffset? ExpirationTime => _context.ExpirationTime;

    public Uri? SourceAddress => _context.SourceAddress;

    public Uri? DestinationAddress => _context.DestinationAddress;

    public Uri? ResponseAddress => _context.ResponseAddress;

    public Uri? FaultAddress => _context.FaultAddress;

    public DateTimeOffset? SentTime => _context.SentTime;

    public Headers Headers => _context.Headers;

    public HostInfo Host => _context.Host;

    public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
    {
        return _context.ConnectPublishObserver(observer);
    }

    public Task PublishAsync<T>(T message, CancellationToken cancellationToken)
        where T : class
    {
        return _context.PublishAsync(message, cancellationToken);
    }

    public Task PublishAsync<T>(T message, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken)
        where T : class
    {
        return _context.PublishAsync(message, publishPipe, cancellationToken);
    }

    public Task PublishAsync<T>(T message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken)
        where T : class
    {
        return _context.PublishAsync(message, publishPipe, cancellationToken);
    }

    public Task PublishAsync(object message, CancellationToken cancellationToken)
    {
        return _context.PublishAsync(message, cancellationToken);
    }

    public Task PublishAsync(object message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken)
    {
        return _context.PublishAsync(message, publishPipe, cancellationToken);
    }

    public Task PublishAsync(object message, Type messageType, CancellationToken cancellationToken)
    {
        return _context.PublishAsync(message, messageType, cancellationToken);
    }

    public Task PublishAsync(object message, Type messageType, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken)
    {
        return _context.PublishAsync(message, messageType, publishPipe, cancellationToken);
    }

    public Task PublishAsync<T>(object values, CancellationToken cancellationToken)
        where T : class
    {
        return _context.PublishAsync<T>(values, cancellationToken);
    }

    public Task PublishAsync<T>(object values, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken)
        where T : class
    {
        return _context.PublishAsync(values, publishPipe, cancellationToken);
    }

    public Task PublishAsync<T>(object values, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken)
        where T : class
    {
        return _context.PublishAsync<T>(values, publishPipe, cancellationToken);
    }

    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        return _context.ConnectSendObserver(observer);
    }

    public Task<ISendEndpoint> GetSendEndpointAsync(Uri address, CancellationToken cancellationToken = default)
    {
        return _context.GetSendEndpointAsync(address, cancellationToken: cancellationToken);
    }

    public ReceiveContext ReceiveContext => _context.ReceiveContext;
    public SerializerContext SerializerContext => _context.SerializerContext;

    public Task ConsumeCompleted => _context.ConsumeCompleted;

    public IEnumerable<string> SupportedMessageTypes => _context.SupportedMessageTypes;

    public bool HasMessageType(Type messageType)
    {
        return _context.HasMessageType(messageType);
    }

    public bool TryGetMessage<T>([NotNullWhen(true)] out ConsumeContext<T>? consumeContext)
        where T : class
    {
        return _context.TryGetMessage(out consumeContext);
    }

    public void AddConsumeTask(Task task)
    {
        _context.AddConsumeTask(task);
    }

    public Task RespondAsync<T>(T message)
        where T : class
    {
        return _context.RespondAsync(message);
    }

    public Task RespondAsync<T>(T message, IPipe<SendContext<T>> sendPipe)
        where T : class
    {
        return _context.RespondAsync(message, sendPipe);
    }

    public Task RespondAsync<T>(T message, IPipe<SendContext> sendPipe)
        where T : class
    {
        return _context.RespondAsync(message, sendPipe);
    }

    public Task RespondAsync(object message)
    {
        return _context.RespondAsync(message);
    }

    public Task RespondAsync(object message, Type messageType)
    {
        return _context.RespondAsync(message, messageType);
    }

    public Task RespondAsync(object message, IPipe<SendContext> sendPipe)
    {
        return _context.RespondAsync(message, sendPipe);
    }

    public Task RespondAsync(object message, Type messageType, IPipe<SendContext> sendPipe)
    {
        return _context.RespondAsync(message, messageType, sendPipe);
    }

    public Task RespondAsync<T>(object values)
        where T : class
    {
        return ResponseAsyncWithMessageAsync<T>(values);
    }

    public Task RespondAsync<T>(object values, IPipe<SendContext<T>> sendPipe)
        where T : class
    {
        return ResponseAsyncWithMessageAsync(values, sendPipe);
    }

    public Task RespondAsync<T>(object values, IPipe<SendContext> sendPipe)
        where T : class
    {
        return ResponseAsyncWithMessageAsync<T>(values, sendPipe);
    }

    public void DeferResponse<T>(T message)
        where T : class
    {
        _context.DeferResponse(message);
    }

    public Task NotifyConsumedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
        where T : class
    {
        return _context.NotifyConsumedAsync(context, duration, consumerType, cancellationToken: cancellationToken);
    }

    public Task NotifyFaultedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
        where T : class
    {
        return _context.NotifyFaultedAsync(context, duration, consumerType, exception, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Initializes the response with the request message, and then uses the initializer to initialize the
    /// remaining properties using the <paramref name="values" /> parameter.
    /// </summary>
    async Task ResponseAsyncWithMessageAsync<T>(object values, IPipe<SendContext<T>>? responsePipe = default)
        where T : class
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        var responseEndpoint = await this.GetResponseEndpointAsync<T>().ConfigureAwait(false);

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
