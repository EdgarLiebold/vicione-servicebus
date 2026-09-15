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

/// <summary>Represents an in-process delivery as a receive context without creating a transport envelope.</summary>
/// <typeparam name="TMessage">The in-process message contract being received.</typeparam>
internal sealed class MediatorReceiveContext<TMessage> :
    ProxyPipeContext,
    ReceiveContext
    where TMessage : class
{
    readonly MediatorConsumeContext<TMessage> _consumeContext;
    readonly MessageIdHeaders _headers;
    readonly Uri _inputAddress;
    readonly MessageBody _messageBody;
    readonly IReceiveObserver _observers;
    readonly PendingTaskCollection _receiveTasks;
    readonly long _receiveStartedAt;
    readonly TimeProvider _timeProvider;

    /// <summary>Creates the receive-side view of a materialized in-process send.</summary>
    /// <param name="sendContext">The materialized message and its send metadata.</param>
    /// <param name="sendEndpointProvider">The provider used for sends initiated while consuming the message.</param>
    /// <param name="publishEndpointProvider">The provider used for publishes initiated while consuming the message.</param>
    /// <param name="publishTopology">The topology used to resolve implemented message contracts.</param>
    /// <param name="observers">The receive observers notified during dispatch.</param>
    /// <param name="objectDeserializer">The deserializer used to project the materialized message to compatible contracts.</param>
    /// <param name="messageBody">The canonical JSON snapshot exposed to the receive pipeline.</param>
    public MediatorReceiveContext(SendContext<TMessage> sendContext, ISendEndpointProvider sendEndpointProvider,
        IPublishEndpointProvider publishEndpointProvider, IPublishTopology publishTopology, IReceiveObserver observers,
        IObjectDeserializer objectDeserializer, MessageBody messageBody)
        : base(sendContext)
    {
        ArgumentNullException.ThrowIfNull(sendContext);
        ArgumentNullException.ThrowIfNull(sendEndpointProvider);
        ArgumentNullException.ThrowIfNull(publishEndpointProvider);
        ArgumentNullException.ThrowIfNull(publishTopology);
        ArgumentNullException.ThrowIfNull(observers);
        ArgumentNullException.ThrowIfNull(objectDeserializer);
        ArgumentNullException.ThrowIfNull(messageBody);

        _observers = observers;
        _inputAddress = sendContext.DestinationAddress
            ?? throw new ArgumentException("A mediator send context must have a destination address.", nameof(sendContext));

        SendEndpointProvider = sendEndpointProvider;
        PublishEndpointProvider = publishEndpointProvider;
        PublishTopology = publishTopology;

        _timeProvider = sendContext.GetTimeProvider();
        _receiveStartedAt = _timeProvider.GetTimestamp();

        var messageId = sendContext.MessageId ?? throw new ArgumentNullException(nameof(MessageContext.MessageId));

        _headers = new MessageIdHeaders(messageId);

        _receiveTasks = new PendingTaskCollection(4);
        _messageBody = messageBody;

        var messageContext = new MediatorSendMessageContext<TMessage>(sendContext);

        var serializationContext = new MediatorSerializationContext<TMessage>(objectDeserializer, messageContext, sendContext.Message,
            MessageTypeCache<TMessage>.MessageTypeNames.ToArray());

        _consumeContext = new MediatorConsumeContext<TMessage>(this, serializationContext, sendContext.Message);

        AddOrUpdatePayload<ConsumeContext>(() => _consumeContext, existing => _consumeContext);
    }

    /// <inheritdoc />
    public IPublishTopology PublishTopology { get; }

    /// <inheritdoc />
    public bool IsDelivered { get; internal set; }
    /// <inheritdoc />
    public bool IsFaulted { get; private set; }

    /// <inheritdoc />
    public bool PublishFaults => false;
    /// <inheritdoc />
    public MessageBody Body => _messageBody;

    /// <inheritdoc />
    public Task ReceiveCompleted => _receiveTasks.CompletedAsync(CancellationToken);

    /// <inheritdoc />
    public void AddReceiveTask(Task task)
    {
        ArgumentNullException.ThrowIfNull(task);
        _receiveTasks.Add(task);
    }

    /// <inheritdoc />
    public ISendEndpointProvider SendEndpointProvider { get; }
    /// <inheritdoc />
    public IPublishEndpointProvider PublishEndpointProvider { get; }

    /// <inheritdoc />
    public bool Redelivered => false;
    /// <inheritdoc />
    public Headers TransportHeaders => _headers;

    /// <inheritdoc />
    public Task NotifyConsumedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerType);
        IsDelivered = true;

        context.LogConsumed(duration, consumerType);

        return _observers.PostConsumeAsync(context, duration, consumerType);
    }

    /// <inheritdoc />
    public Task NotifyFaultedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerType);
        ArgumentNullException.ThrowIfNull(exception);
        IsFaulted = true;

        context.LogFaulted(duration, consumerType, exception);

        GetOrAddPayload<ConsumerFaultContext>(() => new FaultContext(TypeCache<T>.ShortName, consumerType));

        return _observers.ConsumeFaultAsync(context, duration, consumerType, exception);
    }

    /// <inheritdoc />
    public Task NotifyFaultedAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        ArgumentNullException.ThrowIfNull(exception);
        IsFaulted = true;

        this.LogFaulted(exception);

        return _observers.ReceiveFaultAsync(this, exception);
    }

    /// <inheritdoc />
    public TimeSpan ElapsedTime => _timeProvider.GetElapsedTime(_receiveStartedAt);
    /// <inheritdoc />
    public Uri InputAddress => _inputAddress;
    /// <inheritdoc />
    public ContentType ContentType => new(SystemTextJsonRawMessageSerializer.JsonMediaType);

    sealed class FaultContext :
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
