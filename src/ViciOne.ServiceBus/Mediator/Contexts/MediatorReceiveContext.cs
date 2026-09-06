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


/// <summary>Carries state for mediator receive operations.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public sealed class MediatorReceiveContext<TMessage> :
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

    /// <summary>Initializes a new instance.</summary>
    /// <param name="sendContext">The send context.</param>
    /// <param name="sendEndpointProvider">The send endpoint provider.</param>
    /// <param name="publishEndpointProvider">The publish endpoint provider.</param>
    /// <param name="publishTopology">The publish topology.</param>
    /// <param name="observers">The observers.</param>
    /// <param name="objectDeserializer">The object deserializer.</param>
    /// <param name="serializedBodyBytes">The serialized body bytes.</param>
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

    /// <summary>Gets or sets a value indicating whether delivered.</summary>
    public bool IsDelivered { get; internal set; }
    /// <summary>Gets or sets a value indicating whether faulted.</summary>
    public bool IsFaulted { get; private set; }

    /// <summary>Gets the publish faults.</summary>
    public bool PublishFaults => false;
    /// <summary>Gets the body.</summary>
    public MessageBody Body => _messageBody;

    /// <summary>Gets the receive completed.</summary>
    public Task ReceiveCompleted => _receiveTasks.CompletedAsync(CancellationToken);

    /// <summary>Adds receive task to the configuration.</summary>
    /// <param name="task">The task.</param>
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
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); IsDelivered = true;

        context.LogConsumed(duration, consumerType);

        return _observers.PostConsumeAsync(context, duration, consumerType);
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
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); IsFaulted = true;

        context.LogFaulted(duration, consumerType, exception);

        GetOrAddPayload<ConsumerFaultContext>(() => new FaultContext(TypeCache<T>.ShortName, consumerType));

        return _observers.ConsumeFaultAsync(context, duration, consumerType, exception);
    }

    /// <summary>Reports that notify has faulted.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task NotifyFaultedAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); IsFaulted = true;

        this.LogFaulted(exception);

        return _observers.ReceiveFaultAsync(this, exception);
    }

    /// <summary>Gets the elapsed time.</summary>
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
