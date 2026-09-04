using System;
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


public sealed class MediatorReceiveContext<TMessage> :
    ProxyPipeContext,
    ReceiveContext
    where TMessage : class
{
    readonly MediatorConsumeContext<TMessage> _consumeContext;
    readonly MessageIdMessageHeader _headers;
    readonly Uri _inputAddress;
    readonly IReceiveObserver _observers;
    readonly PendingTaskCollection _receiveTasks;
    readonly long _receiveStartedAt;
    readonly TimeProvider _timeProvider;

    public MediatorReceiveContext(SendContext<TMessage> sendContext, ISendEndpointProvider sendEndpointProvider,
        IPublishEndpointProvider publishEndpointProvider, IPublishTopology publishTopology, IReceiveObserver observers,
        IObjectDeserializer objectDeserializer)
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

        var messageContext = new MediatorSendMessageContext<TMessage>(sendContext);

        var serializationContext = new MediatorSerializationContext<TMessage>(objectDeserializer, messageContext, sendContext.Message,
            MessageTypeCache<TMessage>.MessageTypeNames);

        _consumeContext = new MediatorConsumeContext<TMessage>(this, serializationContext, sendContext.Message);

        AddOrUpdatePayload<ConsumeContext>(() => _consumeContext, existing => _consumeContext);
    }

    public IPublishTopology PublishTopology { get; }

    public bool IsDelivered { get; internal set; }
    public bool IsFaulted { get; private set; }

    public bool PublishFaults => false;
    public MessageBody Body => new NotSupportedMessageBody();

    public Task ReceiveCompleted => _receiveTasks.CompletedAsync(CancellationToken);

    public void AddReceiveTask(Task task)
    {
        _receiveTasks.Add(task);
    }

    public ISendEndpointProvider SendEndpointProvider { get; }
    public IPublishEndpointProvider PublishEndpointProvider { get; }

    public bool Redelivered => false;
    public Headers TransportHeaders => _headers;

    public Task NotifyConsumedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); IsDelivered = true;

        context.LogConsumed(duration, consumerType);

        return _observers.PostConsumeAsync(context, duration, consumerType);
    }

    public Task NotifyFaultedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); IsFaulted = true;

        context.LogFaulted(duration, consumerType, exception);

        GetOrAddPayload<ConsumerFaultContext>(() => new FaultContext(TypeCache<T>.ShortName, consumerType));

        return _observers.ConsumeFaultAsync(context, duration, consumerType, exception);
    }

    public Task NotifyFaultedAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); IsFaulted = true;

        this.LogFaulted(exception);

        return _observers.ReceiveFaultAsync(this, exception);
    }

    public TimeSpan ElapsedTime => _timeProvider.GetElapsedTime(_receiveStartedAt);
    public Uri InputAddress => _inputAddress;
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
