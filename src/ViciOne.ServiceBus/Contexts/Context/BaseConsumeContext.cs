using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

#nullable enable
namespace ViciOne.ServiceBus.Context;

public abstract class BaseConsumeContext :
    PublishEndpoint,
    ConsumeContext
{
    protected BaseConsumeContext(ReceiveContext receiveContext, SerializerContext serializerContext)
        : base(receiveContext.PublishEndpointProvider)
    {
        ReceiveContext = receiveContext;
        SerializerContext = serializerContext;
    }

    public virtual CancellationToken CancellationToken => ReceiveContext.CancellationToken;

    public abstract bool HasPayloadType(Type payloadType);

    public abstract bool TryGetPayload<T>([NotNullWhen(true)] out T? payload)
        where T : class;

    public abstract T GetOrAddPayload<T>(PayloadFactory<T> payloadFactory)
        where T : class;

    public abstract T AddOrUpdatePayload<T>(PayloadFactory<T> addFactory, UpdatePayloadFactory<T> updateFactory)
        where T : class;

    public ReceiveContext ReceiveContext { get; protected set; }

    public SerializerContext SerializerContext { get; }

    public abstract Task ConsumeCompleted { get; }

    public abstract Guid? MessageId { get; }
    public abstract Guid? RequestId { get; }
    public abstract Guid? CorrelationId { get; }
    public abstract Guid? ConversationId { get; }
    public abstract Guid? InitiatorId { get; }
    public abstract DateTimeOffset? ExpirationTime { get; }
    public abstract Uri? SourceAddress { get; }
    public abstract Uri? DestinationAddress { get; }
    public abstract Uri? ResponseAddress { get; }
    public abstract Uri? FaultAddress { get; }
    public abstract DateTimeOffset? SentTime { get; }
    public abstract Headers Headers { get; }
    public abstract HostInfo Host { get; }
    public abstract IEnumerable<string> SupportedMessageTypes { get; }
    public abstract bool HasMessageType(Type messageType);

    public abstract bool TryGetMessage<T>([NotNullWhen(true)] out ConsumeContext<T>? consumeContext)
        where T : class;

    public virtual Task RespondAsync<T>(T message)
        where T : class
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        return ConsumeTaskAsync(RespondInternalAsync(message));
    }

    public virtual Task RespondAsync<T>(T message, IPipe<SendContext<T>> sendPipe)
        where T : class
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (sendPipe == null)
            throw new ArgumentNullException(nameof(sendPipe));

        return ConsumeTaskAsync(RespondInternalAsync(message, sendPipe));
    }

    public virtual Task RespondAsync<T>(T message, IPipe<SendContext> sendPipe)
        where T : class
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (sendPipe == null)
            throw new ArgumentNullException(nameof(sendPipe));

        return ConsumeTaskAsync(RespondInternalAsync(message, sendPipe));
    }

    public virtual Task RespondAsync(object message)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var messageType = message.GetType();

        return ResponseEndpointConverterCache.RespondAsync(this, message, messageType);
    }

    public virtual Task RespondAsync(object message, Type messageType)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (messageType == null)
            throw new ArgumentNullException(nameof(messageType));

        return ResponseEndpointConverterCache.RespondAsync(this, message, messageType);
    }

    public virtual Task RespondAsync(object message, IPipe<SendContext> sendPipe)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        if (sendPipe == null)
            throw new ArgumentNullException(nameof(sendPipe));

        var messageType = message.GetType();

        return ResponseEndpointConverterCache.RespondAsync(this, message, messageType, sendPipe);
    }

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

    public virtual Task RespondAsync<T>(object values)
        where T : class
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        return ConsumeTaskAsync(RespondInternalAsync<T>(values));
    }

    public virtual Task RespondAsync<T>(object values, IPipe<SendContext<T>> sendPipe)
        where T : class
    {
        return ConsumeTaskAsync(RespondInternalAsync(values, sendPipe));
    }

    public virtual Task RespondAsync<T>(object values, IPipe<SendContext> sendPipe)
        where T : class
    {
        return ConsumeTaskAsync(RespondInternalAsync<T>(values, sendPipe));
    }

    public virtual void DeferResponse<T>(T message)
        where T : class
    {
        AddConsumeTask(RespondInternalAsync(message));
    }

    public virtual async Task<ISendEndpoint> GetSendEndpointAsync(Uri address, CancellationToken cancellationToken = default)
    {
        var sendEndpoint = await ReceiveContext.SendEndpointProvider.GetSendEndpointAsync(address, cancellationToken: cancellationToken).ConfigureAwait(false);

        return new ConsumeSendEndpoint(sendEndpoint, this);
    }

    public virtual Task NotifyConsumedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
        where T : class
    {
        return ReceiveContext.NotifyConsumedAsync(context, duration, consumerType, cancellationToken: cancellationToken);
    }

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

    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        return ReceiveContext.SendEndpointProvider.ConnectSendObserver(observer);
    }

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

    protected override async Task<ISendEndpoint> GetPublishSendEndpointAsync<T>()
    {
        var publishSendEndpoint = await base.GetPublishSendEndpointAsync<T>().ConfigureAwait(false);

        return new ConsumeSendEndpoint(publishSendEndpoint, this);
    }
}
