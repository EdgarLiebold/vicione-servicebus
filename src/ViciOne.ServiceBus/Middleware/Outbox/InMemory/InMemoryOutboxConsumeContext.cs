using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Middleware.Outbox;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Middleware.Outbox.InMemory;

/// <summary>Persists consume and outgoing-message progress in a process-local inbox entry.</summary>
/// <typeparam name="TMessage">The consumed message contract.</typeparam>
internal sealed class InMemoryOutboxConsumeContext<TMessage> :
    OutboxConsumeContextProxy<TMessage>
    where TMessage : class
{
    readonly InMemoryInboxMessage _inboxMessage;

    /// <summary>Initializes an outbox context over an exclusively owned inbox entry.</summary>
    /// <param name="context">The incoming message context.</param>
    /// <param name="options">The inbox identity and outbox delivery settings.</param>
    /// <param name="provider">The scoped service provider exposed through the context.</param>
    /// <param name="inboxMessage">The inbox entry that stores consume and delivery progress.</param>
    public InMemoryOutboxConsumeContext(ConsumeContext<TMessage> context, OutboxConsumeOptions options, IServiceProvider provider,
        InMemoryInboxMessage inboxMessage)
        : base(context, options, provider)
    {
        _inboxMessage = inboxMessage ?? throw new ArgumentNullException(nameof(inboxMessage));
    }

    /// <summary>Gets or sets whether the receive pipeline may continue after outbox processing.</summary>
    public override bool ContinueProcessing { get; set; } = true;

    /// <summary>Gets a value indicating whether consumption has been committed.</summary>
    public override bool IsMessageConsumed => _inboxMessage.Consumed.HasValue;
    /// <summary>Gets a value indicating whether every captured message has been delivered.</summary>
    public override bool IsOutboxDelivered => _inboxMessage.Delivered.HasValue;
    /// <summary>Gets the number of delivery attempts for the inbox entry.</summary>
    public override int ReceiveCount => _inboxMessage.ReceiveCount;
    /// <summary>Gets the sequence number of the last successfully delivered outgoing message.</summary>
    public override long? LastSequenceNumber => _inboxMessage.LastSequenceNumber;

    Guid InboxMessageId => _inboxMessage.MessageId;

    /// <summary>Marks consumption as committed at the current bus time.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A completed task after the timestamp has been stored.</returns>
    public override Task SetConsumedAsync(CancellationToken cancellationToken = default)
    {
        CancellationToken.ThrowIfCancellationRequested();
        cancellationToken.ThrowIfCancellationRequested();
        _inboxMessage.Consumed = this.GetTimeProvider().GetUtcNow();

        LogContext.Debug?.Log("Outbox Consumed: {MessageId} {Consumed}", InboxMessageId, _inboxMessage.Consumed);
        return Task.CompletedTask;
    }

    /// <summary>Marks every captured message as delivered at the current bus time.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A completed task after the timestamp has been stored.</returns>
    public override Task SetDeliveredAsync(CancellationToken cancellationToken = default)
    {
        CancellationToken.ThrowIfCancellationRequested();
        cancellationToken.ThrowIfCancellationRequested();
        _inboxMessage.Delivered = this.GetTimeProvider().GetUtcNow();

        LogContext.Debug?.Log("Outbox Delivered: {MessageId} {Delivered}", InboxMessageId, _inboxMessage.Delivered);
        return Task.CompletedTask;
    }

    /// <summary>Loads and materializes messages that have not yet been delivered.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task containing the ordered messages awaiting delivery.</returns>
    public override Task<List<OutboxMessageContext>> LoadOutboxMessagesAsync(CancellationToken cancellationToken = default)
    {
        if (CancellationToken.IsCancellationRequested)
            return Task.FromCanceled<List<OutboxMessageContext>>(CancellationToken);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<List<OutboxMessageContext>>(cancellationToken);

        List<InMemoryOutboxMessage> messages = _inboxMessage.GetOutboxMessages();

        for (var i = 0; i < messages.Count; i++)
            messages[i].Deserialize(SerializerContext);

        return Task.FromResult(messages.Cast<OutboxMessageContext>().ToList());
    }

    /// <summary>Advances the durable delivery position to a delivered message.</summary>
    /// <param name="message">The delivered outbox message.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A completed task after the delivery position has been advanced.</returns>
    public override Task NotifyOutboxMessageDeliveredAsync(OutboxMessageContext message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (CancellationToken.IsCancellationRequested)
            return Task.FromCanceled(CancellationToken);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        _inboxMessage.LastSequenceNumber = message.SequenceNumber;

        return Task.CompletedTask;
    }

    /// <summary>Removes every committed outgoing message from the inbox entry.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A completed task after the messages have been removed.</returns>
    public override Task RemoveOutboxMessagesAsync(CancellationToken cancellationToken = default)
    {
        CancellationToken.ThrowIfCancellationRequested();
        cancellationToken.ThrowIfCancellationRequested();
        List<InMemoryOutboxMessage> messages = _inboxMessage.GetOutboxMessages();

        _inboxMessage.RemoveOutboxMessages();

        if (messages.Count > 0)
            LogContext.Debug?.Log("Outbox removed {Count} messages: {MessageId}", messages.Count, InboxMessageId);

        return Task.CompletedTask;
    }

    internal void DiscardPendingConsumerMessages()
    {
        _inboxMessage.RemoveOutboxMessages();
        _inboxMessage.LastSequenceNumber = null;
    }

    /// <summary>Serializes and appends an outgoing send to the inbox entry.</summary>
    /// <typeparam name="T">The outgoing message contract.</typeparam>
    /// <param name="context">The populated send context to persist.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A completed task after the serialized message has been appended.</returns>
    public override Task AddSendAsync<T>(SendContext<T> context, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        CancellationToken.ThrowIfCancellationRequested();
        CancellationToken operationCancellationToken = cancellationToken.CanBeCanceled
            ? cancellationToken
            : context.CancellationToken;
        operationCancellationToken.ThrowIfCancellationRequested();
        if (context.MessageId.HasValue == false)
            throw new MessageException(typeof(T), "The SendContext MessageId must be present");

        var body = context.Serializer.GetMessageBody(context);

        var now = context.GetTimeProvider().GetUtcNow().UtcDateTime;

        var outboxMessage = new InMemoryOutboxMessage
        {
            MessageId = context.MessageId.Value,
            ConversationId = context.ConversationId,
            CorrelationId = context.CorrelationId,
            InitiatorId = context.InitiatorId,
            RequestId = context.RequestId,
            SourceAddress = context.SourceAddress,
            DestinationAddress = context.DestinationAddress,
            ResponseAddress = context.ResponseAddress,
            FaultAddress = context.FaultAddress,
            SentTime = context.SentTime ?? now,
            ContentType = context.ContentType?.ToString() ?? context.Serialization.DefaultContentType.ToString(),
            MessageType = string.Join(";", context.SupportedMessageTypes),
            Body = body.GetRequiredTransportText()
        };

        if (context.TimeToLive.HasValue)
            outboxMessage.ExpirationTime = now + context.TimeToLive;

        if (context.Delay.HasValue)
            outboxMessage.EnqueueTime = now + context.Delay;

        outboxMessage.Headers = SerializerContext.SerializeDictionary(context.Headers.GetAll());

        if (context is TransportSendContext<T> transportSendContext)
        {
            var properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            transportSendContext.WritePropertiesTo(properties);
            outboxMessage.Properties = SerializerContext.SerializeDictionary(properties);
        }

        CancellationToken.ThrowIfCancellationRequested();
        operationCancellationToken.ThrowIfCancellationRequested();
        _inboxMessage.AddOutboxMessage(outboxMessage);
        return Task.CompletedTask;
    }
}
