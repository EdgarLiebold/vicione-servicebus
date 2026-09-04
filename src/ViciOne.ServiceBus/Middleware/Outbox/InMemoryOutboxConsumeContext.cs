using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Middleware.Outbox;

public class InMemoryOutboxConsumeContext<TMessage> :
    OutboxConsumeContextProxy<TMessage>
    where TMessage : class
{
    readonly InMemoryInboxMessage _inboxMessage;

    public InMemoryOutboxConsumeContext(ConsumeContext<TMessage> context, OutboxConsumeOptions options, IServiceProvider provider,
        InMemoryInboxMessage inboxMessage)
        : base(context, options, provider)
    {
        _inboxMessage = inboxMessage;
    }

    public override bool ContinueProcessing { get; set; } = true;

    public override bool IsMessageConsumed => _inboxMessage.Consumed.HasValue;
    public override bool IsOutboxDelivered => _inboxMessage.Delivered.HasValue;
    public override int ReceiveCount => _inboxMessage.ReceiveCount;
    public override long? LastSequenceNumber => _inboxMessage.LastSequenceNumber;

    Guid InboxMessageId => _inboxMessage.MessageId;

    public override async Task SetConsumedAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); _inboxMessage.Consumed = this.GetTimeProvider().GetUtcNow().UtcDateTime;

        LogContext.Debug?.Log("Outbox Consumed: {MessageId} {Consumed}", InboxMessageId, _inboxMessage.Consumed);
    }

    public override async Task SetDeliveredAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); _inboxMessage.Delivered = this.GetTimeProvider().GetUtcNow().UtcDateTime;

        LogContext.Debug?.Log("Outbox Delivered: {MessageId} {Delivered}", InboxMessageId, _inboxMessage.Delivered);
    }

    public override Task<List<OutboxMessageContext>> LoadOutboxMessagesAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::System.Collections.Generic.List<global::ViciOne.ServiceBus.Middleware.OutboxMessageContext>>(cancellationToken); List<InMemoryOutboxMessage> messages = _inboxMessage.GetOutboxMessages();

        for (var i = 0; i < messages.Count; i++)
            messages[i].Deserialize(SerializerContext);

        return Task.FromResult(messages.Cast<OutboxMessageContext>().ToList());
    }

    public override Task NotifyOutboxMessageDeliveredAsync(OutboxMessageContext message, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); _inboxMessage.LastSequenceNumber = message.SequenceNumber;

        return Task.CompletedTask;
    }

    public override async Task RemoveOutboxMessagesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); List<InMemoryOutboxMessage> messages = _inboxMessage.GetOutboxMessages();

        _inboxMessage.RemoveOutboxMessages();

        if (messages.Count > 0)
            LogContext.Debug?.Log("Outbox removed {Count} messages: {MessageId}", messages.Count, InboxMessageId);
    }

    internal Task DiscardPendingConsumerMessagesAsync()
    {
        _inboxMessage.RemoveOutboxMessages();
        _inboxMessage.LastSequenceNumber = null;

        return Task.CompletedTask;
    }

    public override async Task AddSendAsync<T>(SendContext<T> context, CancellationToken cancellationToken = default)
        where T : class
    {
        cancellationToken.ThrowIfCancellationRequested(); if (context.MessageId.HasValue == false)
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
            Body = body.GetString()
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

        _inboxMessage.AddOutboxMessage(outboxMessage);
    }
}
