using System;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Mediator.Contexts;

static class MediatorReceiveContext
{
    const string ContentTypeHeaderValue = "application/vnd.vicione.servicebus+obj";
    internal static readonly ContentType ObjectContentType = new ContentType(ContentTypeHeaderValue);
}


/// <summary>Represents an in-process delivery as a receive context without creating a transport envelope.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
internal sealed class MediatorReceiveContext<TMessage> :
    ProxyPipeContext,
    ReceiveContext
    where TMessage : class
{
    readonly MediatorConsumeContext<TMessage> _consumeContext;
    readonly MessageIdMessageHeader _headers;
    readonly Uri _inputAddress;
    readonly MessageBody _messageBody;
    readonly IReceiveObserver _observers;
    readonly PendingTaskCollection _receiveTasks;
    readonly long _receiveStartedAt;
    readonly TimeProvider _timeProvider;

    /// <summary>Creates the receive-side view of a measured in-process send.</summary>
    /// <param name="sendContext">The materialized message and its send metadata.</param>
    /// <param name="sendEndpointProvider">The provider used for sends initiated while consuming the message.</param>
    /// <param name="publishEndpointProvider">The provider used for publishes initiated while consuming the message.</param>
    /// <param name="publishTopology">The topology used to resolve implemented message contracts.</param>
    /// <param name="observers">The receive observers notified during dispatch.</param>
    /// <param name="objectDeserializer">The deserializer used to project the materialized message to compatible contracts.</param>
    /// <param name="serializedBodyBytes">The measured canonical JSON body length.</param>
    public MediatorReceiveContext(SendContext<TMessage> sendContext, ISendEndpointProvider sendEndpointProvider,
        IPublishEndpointProvider publishEndpointProvider, IPublishTopology publishTopology, IReceiveObserver observers,
        IObjectDeserializer objectDeserializer, long serializedBodyBytes)
        : base(sendContext)
    {
        _observers = observers;
        _inputAddress = sendContext.DestinationAddress
            ?? throw new ArgumentException("A mediator send context must have a destination address.", nameof(sendContext));

        SendEndpointProvider = sendEndpointProvider;
        PublishEndpointProvider = publishEndpointProvider;
        PublishTopology = publishTopology;

        _timeProvider = sendContext.GetTimeProvider();
        _receiveStartedAt = _timeProvider.GetTimestamp();

        var messageId = sendContext.MessageId ?? throw new ArgumentNullException(nameof(MessageContext.MessageId));

        _headers = new MessageIdMessageHeader(messageId);

        _receiveTasks = new PendingTaskCollection(4);
        _messageBody = new MeasuredMediatorMessageBody(serializedBodyBytes);

        var messageContext = new MediatorSendMessageContext<TMessage>(sendContext);

        var serializationContext = new MediatorSerializationContext<TMessage>(objectDeserializer, messageContext, sendContext.Message,
            MessageTypeCache<TMessage>.MessageTypeNames.ToArray());

        _consumeContext = new MediatorConsumeContext<TMessage>(this, serializationContext, sendContext.Message);

        AddOrUpdatePayload<ConsumeContext>(() => _consumeContext, existing => _consumeContext);
    }

    /// <summary>Gets the publish topology.</summary>
    public IPublishTopology PublishTopology { get; }

    /// <summary>Gets whether at least one consumer accepted the delivery.</summary>
    public bool IsDelivered { get; internal set; }
    /// <summary>Gets whether receive or consumer processing faulted.</summary>
    public bool IsFaulted { get; private set; }

    /// <summary>Gets whether mediator receive faults should be republished as transport fault messages.</summary>
    public bool PublishFaults => false;
    /// <summary>Gets the measured, non-materialized receive body.</summary>
    public MessageBody Body => _messageBody;

    /// <summary>Gets a task that completes after every task attached to this delivery.</summary>
    public Task ReceiveCompleted => _receiveTasks.CompletedAsync(CancellationToken);

    /// <summary>Adds asynchronous work whose completion belongs to this delivery.</summary>
    /// <param name="task">The task to await before receive completion.</param>
    public void AddReceiveTask(Task task)
    {
        _receiveTasks.Add(task);
    }

    /// <summary>Gets the send endpoint provider.</summary>
    public ISendEndpointProvider SendEndpointProvider { get; }
    /// <summary>Gets the publish endpoint provider.</summary>
    public IPublishEndpointProvider PublishEndpointProvider { get; }

    /// <summary>Gets the redelivered.</summary>
    public bool Redelivered => false;
    /// <summary>Gets the transport headers.</summary>
    public Headers TransportHeaders => _headers;

    /// <summary>Marks the delivery successful and notifies consume observers.</summary>
    /// <typeparam name="T">The consumed message contract.</typeparam>
    /// <param name="context">The completed consume context.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="consumerType">The runtime consumer type used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task NotifyConsumedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        IsDelivered = true;

        context.LogConsumed(duration, consumerType);

        return _observers.PostConsumeAsync(context, duration, consumerType);
    }

    /// <summary>Marks consumer processing faulted and notifies consume observers.</summary>
    /// <typeparam name="T">The faulted message contract.</typeparam>
    /// <param name="context">The faulted consume context.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="consumerType">The runtime consumer type used by the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task NotifyFaultedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        IsFaulted = true;

        context.LogFaulted(duration, consumerType, exception);

        GetOrAddPayload<ConsumerFaultContext>(() => new FaultContext(TypeCache<T>.ShortName, consumerType));

        return _observers.ConsumeFaultAsync(context, duration, consumerType, exception);
    }

    /// <summary>Marks receive processing faulted and notifies receive observers.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task NotifyFaultedAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        IsFaulted = true;

        this.LogFaulted(exception);

        return _observers.ReceiveFaultAsync(this, exception);
    }

    /// <summary>Gets the elapsed receive time measured by the configured time provider.</summary>
    public TimeSpan ElapsedTime => _timeProvider.GetElapsedTime(_receiveStartedAt);
    /// <summary>Gets the input address.</summary>
    public Uri InputAddress => _inputAddress;
    /// <summary>Gets the content type.</summary>
    public ContentType ContentType => MediatorReceiveContext.ObjectContentType;


    class FaultContext :
        ConsumerFaultContext
    {
        public FaultContext(string messageType, string consumerType)
        {
            MessageType = messageType;
            ConsumerType = consumerType;
        }

        public string MessageType { get; }
        public string ConsumerType { get; }
    }
}

sealed class MeasuredMediatorMessageBody : MessageBody
{
    public MeasuredMediatorMessageBody(long length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        Length = length;
    }

    public long? Length { get; }

    public Stream GetStream() => throw new NotSupportedException("The in-process mediator has no serialized receive stream.");

    public byte[] GetBytes() => throw new NotSupportedException("The in-process mediator has no serialized receive body.");

    public string GetString() => throw new NotSupportedException("The in-process mediator has no serialized receive text.");
}
