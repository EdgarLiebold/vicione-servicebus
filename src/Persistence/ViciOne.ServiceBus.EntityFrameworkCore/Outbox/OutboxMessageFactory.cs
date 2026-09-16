using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

internal static class OutboxMessageFactory
{
    public static OutboxMessage Create<T>(
        SendContext<T> context,
        IObjectDeserializer deserializer,
        TimeProvider timeProvider,
        Guid? inboxMessageId = null,
        Guid? inboxConsumerId = null,
        Guid? outboxId = null)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(deserializer);
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (context.MessageId is not { } messageId || messageId == Guid.Empty)
            throw new MessageException(typeof(T), "The SendContext MessageId must be present and nonempty");

        ValidateOwner(inboxMessageId, inboxConsumerId, outboxId);

        var body = context.Serializer.GetMessageBody(context);
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        var outboxMessage = new OutboxMessage
        {
            MessageId = messageId,
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
            Body = body.GetRequiredTransportText(),
            InboxMessageId = inboxMessageId,
            InboxConsumerId = inboxConsumerId,
            OutboxId = outboxId,
        };

        if (context.TimeToLive.HasValue)
            outboxMessage.ExpirationTime = now + context.TimeToLive;

        if (context.Delay.HasValue)
            outboxMessage.EnqueueTime = now + context.Delay;

        outboxMessage.Headers = deserializer.SerializeDictionary(context.Headers.GetAll());

        if (context is TransportSendContext<T> transportSendContext)
        {
            var properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            transportSendContext.WritePropertiesTo(properties);
            outboxMessage.Properties = deserializer.SerializeDictionary(properties);
        }

        return outboxMessage;
    }

    static void ValidateOwner(Guid? inboxMessageId, Guid? inboxConsumerId, Guid? outboxId)
    {
        if (inboxMessageId == Guid.Empty)
            throw new ArgumentException("The inbox message identifier must be nonempty.", nameof(inboxMessageId));
        if (inboxConsumerId == Guid.Empty)
            throw new ArgumentException("The inbox consumer identifier must be nonempty.", nameof(inboxConsumerId));
        if (outboxId == Guid.Empty)
            throw new ArgumentException("The transactional outbox identifier must be nonempty.", nameof(outboxId));

        bool hasInboxOwner = inboxMessageId.HasValue && inboxConsumerId.HasValue;
        if (inboxMessageId.HasValue != inboxConsumerId.HasValue)
            throw new ArgumentException("Inbox ownership requires both the message and consumer identifiers.");
        if (hasInboxOwner == outboxId.HasValue)
            throw new ArgumentException("An outbox message must have exactly one inbox or transactional-outbox owner.");
    }
}
