using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Context;

/// <summary>Carries state for base consume operations.</summary>
public abstract class BaseConsumeContext :
    PublishEndpoint,
    ConsumeContext
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="receiveContext">The receive context.</param>
    /// <param name="serializerContext">The serializer context.</param>
    protected BaseConsumeContext(ReceiveContext receiveContext, SerializerContext serializerContext)
        : base(receiveContext.PublishEndpointProvider)
    {
        ReceiveContext = receiveContext;
        SerializerContext = serializerContext;
    }

    /// <summary>Gets the cancellation token.</summary>
    public virtual CancellationToken CancellationToken => ReceiveContext.CancellationToken;

    /// <summary>Determines whether the current value has payload type.</summary>
    /// <param name="payloadType">The runtime payload type used by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public abstract bool HasPayloadType(Type payloadType);

    /// <summary>Attempts to get payload.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="payload">Receives the payload produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public abstract bool TryGetPayload<T>([NotNullWhen(true)] out T? payload)
        where T : class;

    /// <summary>Gets or add payload.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="payloadFactory">The payload factory.</param>
    /// <returns>The or add payload.</returns>
    public abstract T GetOrAddPayload<T>(PayloadFactory<T> payloadFactory)
        where T : class;

    /// <summary>Adds or update payload to the configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="addFactory">The add factory.</param>
    /// <param name="updateFactory">The update factory.</param>
    /// <returns>The t produced by the operation.</returns>
    public abstract T AddOrUpdatePayload<T>(PayloadFactory<T> addFactory, UpdatePayloadFactory<T> updateFactory)
        where T : class;

    /// <summary>Gets or sets the receive context.</summary>
    public ReceiveContext ReceiveContext { get; protected set; }

    /// <summary>Gets the serializer context.</summary>
    public SerializerContext SerializerContext { get; }

    /// <summary>Gets the consume completed.</summary>
    public abstract Task ConsumeCompleted { get; }

    /// <summary>Gets the outgoing.</summary>
    public IOutgoingMessages Outgoing => new ConsumeContextOutgoingMessages(this);

    /// <summary>Gets the message id.</summary>
    public abstract Guid? MessageId { get; }
    /// <summary>Gets the request id.</summary>
    public abstract Guid? RequestId { get; }
    /// <summary>Gets the correlation id.</summary>
    public abstract Guid? CorrelationId { get; }
    /// <summary>Gets the conversation id.</summary>
    public abstract Guid? ConversationId { get; }
    /// <summary>Gets the initiator id.</summary>
    public abstract Guid? InitiatorId { get; }
    /// <summary>Gets the expiration time.</summary>
    public abstract DateTimeOffset? ExpirationTime { get; }
    /// <summary>Gets the source address.</summary>
    public abstract Uri? SourceAddress { get; }
    /// <summary>Gets the destination address.</summary>
    public abstract Uri? DestinationAddress { get; }
    /// <summary>Gets the response address.</summary>
    public abstract Uri? ResponseAddress { get; }
    /// <summary>Gets the fault address.</summary>
    public abstract Uri? FaultAddress { get; }
    /// <summary>Gets the sent time.</summary>
    public abstract DateTimeOffset? SentTime { get; }
    /// <summary>Gets the headers.</summary>
    public abstract Headers Headers { get; }
    /// <summary>Gets the host.</summary>
    public abstract HostInfo Host { get; }
    /// <summary>Gets the supported message types.</summary>
    public abstract IEnumerable<string> SupportedMessageTypes { get; }
    /// <summary>Determines whether the current value has message type.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public abstract bool HasMessageType(Type messageType);

    /// <summary>Attempts to get message.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="consumeContext">Receives the consume context produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public abstract bool TryGetMessage<T>([NotNullWhen(true)] out ConsumeContext<T>? consumeContext)
        where T : class;

    /// <summary>Sends the configured response.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual Task RespondAsync<T>(T message)
        where T : class
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        return ConsumeTaskAsync(RespondInternalAsync(message));
    }

    /// <summary>Responds to the consumed message with application-level send options.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A task that represents the response operation.</returns>
    public virtual Task RespondAsync<T>(T message, SendOptions options)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(options);

        return RespondAsync(message, new SendOptionsPipe<T>(options));
    }

    /// <summary>Sends the configured response.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="sendPipe">The send pipe.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual Task RespondAsync<T>(T message, IPipe<SendContext<T>> sendPipe)
        where T : class
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (sendPipe == null)
            throw new ArgumentNullException(nameof(sendPipe));

        return ConsumeTaskAsync(RespondInternalAsync(message, sendPipe));
    }

    /// <summary>Sends the configured response.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="sendPipe">The send pipe.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual Task RespondAsync<T>(T message, IPipe<SendContext> sendPipe)
        where T : class
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (sendPipe == null)
            throw new ArgumentNullException(nameof(sendPipe));

        return ConsumeTaskAsync(RespondInternalAsync(message, sendPipe));
    }

    /// <summary>Sends the configured response.</summary>
    /// <param name="message">The message to process.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual Task RespondAsync(object message)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var messageType = message.GetType();

        return ResponseEndpointConverterCache.RespondAsync(this, message, messageType);
    }

    /// <summary>Sends the configured response.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual Task RespondAsync(object message, Type messageType)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (messageType == null)
            throw new ArgumentNullException(nameof(messageType));

        return ResponseEndpointConverterCache.RespondAsync(this, message, messageType);
    }

    /// <summary>Sends the configured response.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="sendPipe">The send pipe.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual Task RespondAsync(object message, IPipe<SendContext> sendPipe)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (sendPipe == null)
            throw new ArgumentNullException(nameof(sendPipe));

        var messageType = message.GetType();

        return ResponseEndpointConverterCache.RespondAsync(this, message, messageType, sendPipe);
    }

    /// <summary>Sends the configured response.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="sendPipe">The send pipe.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual Task RespondAsync(object message, Type messageType, IPipe<SendContext> sendPipe)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (messageType == null)
            throw new ArgumentNullException(nameof(messageType));
        if (sendPipe == null)
            throw new ArgumentNullException(nameof(sendPipe));

        return ResponseEndpointConverterCache.RespondAsync(this, message, messageType, sendPipe);
    }

    /// <summary>Sends the configured response.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="values">The values.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual Task RespondAsync<T>(object values)
        where T : class
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        return ConsumeTaskAsync(RespondInternalAsync<T>(values));
    }

    /// <summary>Sends the configured response.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="values">The values.</param>
    /// <param name="sendPipe">The send pipe.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual Task RespondAsync<T>(object values, IPipe<SendContext<T>> sendPipe)
        where T : class
    {
        return ConsumeTaskAsync(RespondInternalAsync(values, sendPipe));
    }

    /// <summary>Sends the configured response.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="values">The values.</param>
    /// <param name="sendPipe">The send pipe.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual Task RespondAsync<T>(object values, IPipe<SendContext> sendPipe)
        where T : class
    {
        return ConsumeTaskAsync(RespondInternalAsync<T>(values, sendPipe));
    }

    /// <summary>Defers response.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    public virtual void DeferResponse<T>(T message)
        where T : class
    {
        AddConsumeTask(RespondInternalAsync(message));
    }

    /// <summary>Gets send endpoint.</summary>
    /// <param name="address">The address.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public virtual async Task<ISendEndpoint> GetSendEndpointAsync(Uri address, CancellationToken cancellationToken = default)
    {
        var sendEndpoint = await ReceiveContext.SendEndpointProvider.GetSendEndpointAsync(address, cancellationToken: cancellationToken).ConfigureAwait(false);

        return new ConsumeSendEndpoint(sendEndpoint, this);
    }

    /// <summary>Reports that notify has been consumed.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="consumerType">The runtime consumer type used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual Task NotifyConsumedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
        where T : class
    {
        return ReceiveContext.NotifyConsumedAsync(context, duration, consumerType, cancellationToken: cancellationToken);
    }

    /// <summary>Reports that notify has faulted.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="consumerType">The runtime consumer type used by the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual async Task NotifyFaultedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
        where T : class
    {
        switch (exception)
        {
            case OperationCanceledException canceled when canceled.CancellationToken == context.CancellationToken:
                break;

            default:
                if (!context.CancellationToken.IsCancellationRequested)
                    await GenerateFaultAsync(context, exception).ConfigureAwait(false);
                break;
        }

        await ReceiveContext.NotifyFaultedAsync(context, duration, consumerType, exception, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Connects send observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        return ReceiveContext.SendEndpointProvider.ConnectSendObserver(observer);
    }

    /// <summary>Adds consume task to the configuration.</summary>
    /// <param name="task">The task.</param>
    public abstract void AddConsumeTask(Task task);

    Task RespondInternalAsync<T>(T message, IPipe<SendContext<T>>? pipe = null)
        where T : class
    {
        Task<ISendEndpoint> sendEndpointTask = this.GetResponseEndpointAsync<T>();
        if (sendEndpointTask.Status == TaskStatus.RanToCompletion)
        {
            var sendEndpoint = sendEndpointTask.Result;

            return pipe.IsNotEmpty()
                ? sendEndpoint.SendAsync(message, pipe!, CancellationToken)
                : sendEndpoint.SendAsync(message, CancellationToken);
        }

        async Task RespondInternalAsync()
        {
            var sendEndpoint = await sendEndpointTask.ConfigureAwait(false);

            if (pipe.IsNotEmpty())
                await sendEndpoint.SendAsync(message, pipe!, CancellationToken).ConfigureAwait(false);
            else
                await sendEndpoint.SendAsync(message, CancellationToken).ConfigureAwait(false);
        }

        return RespondInternalAsync();
    }

    Task RespondInternalAsync<T>(object values, IPipe<SendContext<T>>? pipe = null)
        where T : class
    {
        Task<ISendEndpoint> sendEndpointTask = this.GetResponseEndpointAsync<T>();
        if (sendEndpointTask.Status == TaskStatus.RanToCompletion)
        {
            var sendEndpoint = sendEndpointTask.Result;

            return pipe.IsNotEmpty()
                ? sendEndpoint.SendAsync(values, pipe!, CancellationToken)
                : sendEndpoint.SendAsync<T>(values, CancellationToken);
        }

        async Task RespondInternalAsync()
        {
            var sendEndpoint = await sendEndpointTask.ConfigureAwait(false);

            if (pipe.IsNotEmpty())
                await sendEndpoint.SendAsync(values, pipe!, CancellationToken).ConfigureAwait(false);
            else
                await sendEndpoint.SendAsync<T>(values, CancellationToken).ConfigureAwait(false);
        }

        return RespondInternalAsync();
    }

    /// <summary>Generates fault.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected virtual Task GenerateFaultAsync<T>(ConsumeContext<T> context, Exception exception)
        where T : class
    {
        return context.GenerateFaultAsync(exception);
    }

    Task ConsumeTaskAsync(Task task)
    {
        AddConsumeTask(task);

        return task;
    }

    /// <summary>Gets publish send endpoint.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>A task that produces the requested value.</returns>
    protected override async Task<ISendEndpoint> GetPublishSendEndpointAsync<T>()
    {
        var publishSendEndpoint = await base.GetPublishSendEndpointAsync<T>().ConfigureAwait(false);

        return new ConsumeSendEndpoint(publishSendEndpoint, this);
    }
}
