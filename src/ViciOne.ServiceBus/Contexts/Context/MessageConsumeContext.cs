using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Initializers;

namespace ViciOne.ServiceBus.Context;

/// <summary>Carries state for message consume operations.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class MessageConsumeContext<TMessage> :
    ConsumeContext<TMessage>,
    ConsumeContext
    where TMessage : class
{
    readonly ConsumeContext _context;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="message">The message to process.</param>
    public MessageConsumeContext(ConsumeContext context, TMessage message)
    {
        _context = context;

        Message = message;
    }

    /// <summary>Gets the message.</summary>
    public TMessage Message { get; }

    /// <summary>Gets the outgoing.</summary>
    public IOutgoingMessages Outgoing => _context.Outgoing;

    /// <summary>Reports that notify has been consumed.</summary>
    /// <param name="duration">The duration.</param>
    /// <param name="consumerType">The runtime consumer type used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task NotifyConsumedAsync(TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
    {
        return _context.NotifyConsumedAsync(this, duration, consumerType, cancellationToken: cancellationToken);
    }

    /// <summary>Reports that notify has faulted.</summary>
    /// <param name="duration">The duration.</param>
    /// <param name="consumerType">The runtime consumer type used by the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task NotifyFaultedAsync(TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
    {
        return _context.NotifyFaultedAsync(this, duration, consumerType, exception, cancellationToken: cancellationToken);
    }

    /// <summary>Determines whether the current value has payload type.</summary>
    /// <param name="payloadType">The runtime payload type used by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool HasPayloadType(Type payloadType)
    {
        return payloadType.IsInstanceOfType(this) || _context.HasPayloadType(payloadType);
    }

    /// <summary>Attempts to get payload.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="payload">Receives the payload produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
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

    /// <summary>Gets or add payload.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="payloadFactory">The payload factory.</param>
    /// <returns>The or add payload.</returns>
    public T GetOrAddPayload<T>(PayloadFactory<T> payloadFactory)
        where T : class
    {
        if (this is T context)
            return context;

        return _context.GetOrAddPayload(payloadFactory);
    }

    /// <summary>Adds or update payload to the configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="addFactory">The add factory.</param>
    /// <param name="updateFactory">The update factory.</param>
    /// <returns>The t produced by the operation.</returns>
    public T AddOrUpdatePayload<T>(PayloadFactory<T> addFactory, UpdatePayloadFactory<T> updateFactory)
        where T : class
    {
        if (this is T context)
            return context;

        return _context.AddOrUpdatePayload(addFactory, updateFactory);
    }

    /// <summary>Gets the cancellation token.</summary>
    public CancellationToken CancellationToken => _context.CancellationToken;

    /// <summary>Gets the message id.</summary>
    public Guid? MessageId => _context.MessageId;

    /// <summary>Gets the request id.</summary>
    public Guid? RequestId => _context.RequestId;

    /// <summary>Gets the correlation id.</summary>
    public Guid? CorrelationId => _context.CorrelationId;

    /// <summary>Gets the conversation id.</summary>
    public Guid? ConversationId => _context.ConversationId;

    /// <summary>Gets the initiator id.</summary>
    public Guid? InitiatorId => _context.InitiatorId;

    /// <summary>Gets the expiration time.</summary>
    public DateTimeOffset? ExpirationTime => _context.ExpirationTime;

    /// <summary>Gets the source address.</summary>
    public Uri? SourceAddress => _context.SourceAddress;

    /// <summary>Gets the destination address.</summary>
    public Uri? DestinationAddress => _context.DestinationAddress;

    /// <summary>Gets the response address.</summary>
    public Uri? ResponseAddress => _context.ResponseAddress;

    /// <summary>Gets the fault address.</summary>
    public Uri? FaultAddress => _context.FaultAddress;

    /// <summary>Gets the sent time.</summary>
    public DateTimeOffset? SentTime => _context.SentTime;

    /// <summary>Gets the headers.</summary>
    public Headers Headers => _context.Headers;

    /// <summary>Gets the host.</summary>
    public HostInfo Host => _context.Host;

    /// <summary>Connects publish observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
    {
        return _context.ConnectPublishObserver(observer);
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PublishAsync<T>(T message, CancellationToken cancellationToken)
        where T : class
    {
        return _context.PublishAsync(message, cancellationToken);
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="publishPipe">The publish pipe.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PublishAsync<T>(T message, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken)
        where T : class
    {
        return _context.PublishAsync(message, publishPipe, cancellationToken);
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="publishPipe">The publish pipe.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PublishAsync<T>(T message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken)
        where T : class
    {
        return _context.PublishAsync(message, publishPipe, cancellationToken);
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PublishAsync(object message, CancellationToken cancellationToken)
    {
        return _context.PublishAsync(message, cancellationToken);
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="publishPipe">The publish pipe.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PublishAsync(object message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken)
    {
        return _context.PublishAsync(message, publishPipe, cancellationToken);
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PublishAsync(object message, Type messageType, CancellationToken cancellationToken)
    {
        return _context.PublishAsync(message, messageType, cancellationToken);
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="publishPipe">The publish pipe.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PublishAsync(object message, Type messageType, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken)
    {
        return _context.PublishAsync(message, messageType, publishPipe, cancellationToken);
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="values">The values.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PublishAsync<T>(object values, CancellationToken cancellationToken)
        where T : class
    {
        return _context.PublishAsync<T>(values, cancellationToken);
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="values">The values.</param>
    /// <param name="publishPipe">The publish pipe.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PublishAsync<T>(object values, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken)
        where T : class
    {
        return _context.PublishAsync(values, publishPipe, cancellationToken);
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="values">The values.</param>
    /// <param name="publishPipe">The publish pipe.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PublishAsync<T>(object values, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken)
        where T : class
    {
        return _context.PublishAsync<T>(values, publishPipe, cancellationToken);
    }

    /// <summary>Connects send observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        return _context.ConnectSendObserver(observer);
    }

    /// <summary>Gets send endpoint.</summary>
    /// <param name="address">The address.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public Task<ISendEndpoint> GetSendEndpointAsync(Uri address, CancellationToken cancellationToken = default)
    {
        return _context.GetSendEndpointAsync(address, cancellationToken: cancellationToken);
    }

    /// <summary>Gets the receive context.</summary>
    public ReceiveContext ReceiveContext => _context.ReceiveContext;
    /// <summary>Gets the serializer context.</summary>
    public SerializerContext SerializerContext => _context.SerializerContext;

    /// <summary>Gets the consume completed.</summary>
    public Task ConsumeCompleted => _context.ConsumeCompleted;

    /// <summary>Gets the supported message types.</summary>
    public IEnumerable<string> SupportedMessageTypes => _context.SupportedMessageTypes;

    /// <summary>Determines whether the current value has message type.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool HasMessageType(Type messageType)
    {
        return _context.HasMessageType(messageType);
    }

    /// <summary>Attempts to get message.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="consumeContext">Receives the consume context produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetMessage<T>([NotNullWhen(true)] out ConsumeContext<T>? consumeContext)
        where T : class
    {
        return _context.TryGetMessage(out consumeContext);
    }

    /// <summary>Adds consume task to the configuration.</summary>
    /// <param name="task">The task.</param>
    public void AddConsumeTask(Task task)
    {
        _context.AddConsumeTask(task);
    }

    /// <summary>Sends the configured response.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task RespondAsync<T>(T message)
        where T : class
    {
        return _context.RespondAsync(message);
    }

    /// <summary>Responds to the consumed message with application-level send options.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A task that represents the response operation.</returns>
    public Task RespondAsync<T>(T message, SendOptions options)
        where T : class
    {
        return _context.RespondAsync(message, options);
    }

    /// <summary>Sends the configured response.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="sendPipe">The send pipe.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task RespondAsync<T>(T message, IPipe<SendContext<T>> sendPipe)
        where T : class
    {
        return _context.RespondAsync(message, sendPipe);
    }

    /// <summary>Sends the configured response.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="sendPipe">The send pipe.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task RespondAsync<T>(T message, IPipe<SendContext> sendPipe)
        where T : class
    {
        return _context.RespondAsync(message, sendPipe);
    }

    /// <summary>Sends the configured response.</summary>
    /// <param name="message">The message to process.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task RespondAsync(object message)
    {
        return _context.RespondAsync(message);
    }

    /// <summary>Sends the configured response.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task RespondAsync(object message, Type messageType)
    {
        return _context.RespondAsync(message, messageType);
    }

    /// <summary>Sends the configured response.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="sendPipe">The send pipe.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task RespondAsync(object message, IPipe<SendContext> sendPipe)
    {
        return _context.RespondAsync(message, sendPipe);
    }

    /// <summary>Sends the configured response.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="sendPipe">The send pipe.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task RespondAsync(object message, Type messageType, IPipe<SendContext> sendPipe)
    {
        return _context.RespondAsync(message, messageType, sendPipe);
    }

    /// <summary>Sends the configured response.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="values">The values.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task RespondAsync<T>(object values)
        where T : class
    {
        return ResponseAsyncWithMessageAsync<T>(values);
    }

    /// <summary>Sends the configured response.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="values">The values.</param>
    /// <param name="sendPipe">The send pipe.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task RespondAsync<T>(object values, IPipe<SendContext<T>> sendPipe)
        where T : class
    {
        return ResponseAsyncWithMessageAsync(values, sendPipe);
    }

    /// <summary>Sends the configured response.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="values">The values.</param>
    /// <param name="sendPipe">The send pipe.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task RespondAsync<T>(object values, IPipe<SendContext> sendPipe)
        where T : class
    {
        return ResponseAsyncWithMessageAsync<T>(values, sendPipe);
    }

    /// <summary>Defers response.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    public void DeferResponse<T>(T message)
        where T : class
    {
        _context.DeferResponse(message);
    }

    /// <summary>Reports that notify has been consumed.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="consumerType">The runtime consumer type used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task NotifyConsumedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
        where T : class
    {
        return _context.NotifyConsumedAsync(context, duration, consumerType, cancellationToken: cancellationToken);
    }

    /// <summary>Reports that notify has faulted.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="consumerType">The runtime consumer type used by the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task NotifyFaultedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
        where T : class
    {
        return _context.NotifyFaultedAsync(context, duration, consumerType, exception, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Initializes the response with the request message, and then uses the initializer to initialize the
    /// remaining properties using the <paramref name="values" /> parameter.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="values">The values.</param>
    /// <param name="responsePipe">The response pipe.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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
